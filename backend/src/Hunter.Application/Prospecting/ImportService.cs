using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Hunter.Application.Common;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Prospecting;

public class ImportService(
    IHunterDbContext db,
    ICurrentUserService currentUser,
    IProspectDuplicateFinder duplicateFinder,
    IGooglePlacesClient googlePlacesClient,
    IOpenStreetMapClient openStreetMapClient,
    IApifyGoogleMapsClient apifyGoogleMapsClient) : IImportService
{
    public async Task<Result<ImportPreviewDto>> ImportCsvAsync(Stream csvStream, string fileName, CancellationToken ct = default)
    {
        var organizationId = currentUser.OrganizationId!.Value;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim
        };

        List<ProspectCsvRow> rows;
        using (var reader = new StreamReader(csvStream))
        using (var csv = new CsvReader(reader, config))
        {
            rows = csv.GetRecords<ProspectCsvRow>().ToList();
        }

        var batch = await BuildBatchAsync(rows, fileName, ProspectSourceType.CsvImport, organizationId, ct);

        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        return Result<ImportPreviewDto>.Success(ToPreviewDto(batch));
    }

    public async Task<Result<ImportPreviewDto>> ImportFromGooglePlacesAsync(GooglePlacesImportRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return Result<ImportPreviewDto>.Failure("La búsqueda no puede estar vacía.");

        var organizationId = currentUser.OrganizationId!.Value;

        var places = await googlePlacesClient.SearchTextAsync(request.Query, request.MaxResults, ct);
        if (places.Count == 0)
            return Result<ImportPreviewDto>.Failure("Google Places no devolvió resultados (o la búsqueda falló).");

        var rows = places.Select(p => new ProspectCsvRow
        {
            business_name = p.Name,
            // Google Places devuelve un único teléfono sin distinguir si es WhatsApp; en Argentina
            // el mismo número suele ser ambos, así que se registra en los dos canales (doc 21, sección 8).
            phone = p.PhoneNumber,
            whatsapp = p.PhoneNumber,
            address = p.FormattedAddress,
            city = p.City,
            province = p.Province,
            source = "google_places"
        }).ToList();

        var batch = await BuildBatchAsync(rows, $"google_places: {request.Query}", ProspectSourceType.GooglePlaces, organizationId, ct);

        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        return Result<ImportPreviewDto>.Success(ToPreviewDto(batch));
    }

    private const int MaxOpenStreetMapLocalities = 5;
    private const int MinOpenStreetMapRadiusKm = 1;
    private const int MaxOpenStreetMapRadiusKm = 50;

    // Radio que se usa cuando hay rubros libres (Keywords) y no se pidió uno explícito. Sin esto,
    // un rubro libre sin radio cae en modo administrativo (busca en todo el partido/municipio vía
    // OpenStreetMapClient.BuildAreaQuery) — un name~regex ahí escanea todo lo que tenga nombre en
    // esa área completa (no solo negocios) y pisa el timeout de Overpass en partidos grandes
    // (confirmado a mano: timeout a los 73s buscando en Morón sin radio). Con radio, en cambio, se
    // usa BuildRadiusQuery (nwr(around:...)), que sí resuelve rápido porque acota geográficamente
    // antes de evaluar el regex.
    private const int DefaultKeywordRadiusKm = 20;

    public async Task<Result<ImportPreviewDto>> ImportFromOpenStreetMapAsync(OpenStreetMapImportRequest request, CancellationToken ct = default)
    {
        var localities = (request.Localities ?? [])
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l!)
            .Distinct()
            .ToList();

        if (localities.Count == 0)
            return Result<ImportPreviewDto>.Failure("Debe indicar al menos una zona o localidad.");
        if (localities.Count > MaxOpenStreetMapLocalities)
            return Result<ImportPreviewDto>.Failure($"Máximo {MaxOpenStreetMapLocalities} localidades por búsqueda.");

        var categories = request.Categories is { Count: > 0 } ? request.Categories.Distinct().ToList() : [];
        var unsupported = categories.Except(OpenStreetMapCategories.Supported).ToList();
        if (unsupported.Count > 0)
            return Result<ImportPreviewDto>.Failure($"Rubro(s) no soportados por OpenStreetMap: {string.Join(", ", unsupported)}.");

        var keywords = (request.Keywords ?? [])
            .Select(k => k?.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k!)
            .Distinct()
            .ToList();

        // Sin categorías ni rubros libres = comportamiento original (se buscan todos los rubros
        // soportados). Si el usuario puso al menos un rubro libre, no se agregan los 5 rubros
        // automotrices de más: solo quiere ese término.
        if (categories.Count == 0 && keywords.Count == 0)
            categories = OpenStreetMapCategories.Supported.ToList();

        var radiusKm = request.RadiusKm ?? (keywords.Count > 0 ? DefaultKeywordRadiusKm : (int?)null);
        if (radiusKm is int radius && (radius < MinOpenStreetMapRadiusKm || radius > MaxOpenStreetMapRadiusKm))
            return Result<ImportPreviewDto>.Failure($"El radio debe estar entre {MinOpenStreetMapRadiusKm} y {MaxOpenStreetMapRadiusKm} km.");

        var organizationId = currentUser.OrganizationId!.Value;

        var criteria = new OpenStreetMapSearchCriteria(localities, categories, radiusKm, request.MaxResults, keywords);
        var places = await openStreetMapClient.SearchAsync(criteria, ct);
        if (places.Count == 0)
            return Result<ImportPreviewDto>.Failure("OpenStreetMap no devolvió resultados (o la búsqueda falló).");

        var rows = places.Select(p => new ProspectCsvRow
        {
            business_name = p.Name,
            phone = p.PhoneNumber,
            // A diferencia de la versión anterior, sí se asume que un teléfono con forma de
            // celular argentino (54 + código de área + número) es WhatsApp-capable aunque OSM no
            // traiga el "9" — ver AssumeWhatsAppCapable. Prioriza no perder leads reales; un
            // eventual fijo real que se cuele falla el envío de forma visible en vez de en
            // silencio (Mensajes > Enviados).
            whatsapp = WhatsAppCapableNumber(p.PhoneNumber),
            address = p.Address,
            city = p.City,
            province = p.Province,
            category = p.Category.ToString(),
            source = "openstreetmap"
        }).ToList();

        var batch = await BuildBatchAsync(
            rows, $"openstreetmap: {string.Join(", ", localities)}", ProspectSourceType.OpenStreetMap, organizationId, ct);

        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        return Result<ImportPreviewDto>.Success(ToPreviewDto(batch));
    }

    private const int MaxApifyLocalities = 5;
    private const int MaxApifyKeywords = 5;

    // Fuente alternativa a OpenStreetMap (ver ProspectSearchPage, selector de fuente): a
    // diferencia de OSM, acá el rubro SIEMPRE es texto libre — Apify scrapea Google Maps por
    // texto, no hay tags cerrados que mapear, así que cualquier rubro que el usuario escriba
    // sirve tal cual. MaxApifyKeywords acota la combinatoria localidades×rubros (cada combinación
    // es una búsqueda separada dentro del actor, y es un servicio pago).
    public async Task<Result<ImportPreviewDto>> ImportFromApifyAsync(ApifyImportRequest request, CancellationToken ct = default)
    {
        var localities = (request.Localities ?? [])
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l!)
            .Distinct()
            .ToList();

        if (localities.Count == 0)
            return Result<ImportPreviewDto>.Failure("Debe indicar al menos una zona o localidad.");
        if (localities.Count > MaxApifyLocalities)
            return Result<ImportPreviewDto>.Failure($"Máximo {MaxApifyLocalities} localidades por búsqueda.");

        var keywords = (request.Keywords ?? [])
            .Select(k => k?.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k!)
            .Distinct()
            .ToList();

        if (keywords.Count == 0)
            return Result<ImportPreviewDto>.Failure("Debe indicar al menos un rubro a buscar.");
        if (keywords.Count > MaxApifyKeywords)
            return Result<ImportPreviewDto>.Failure($"Máximo {MaxApifyKeywords} rubros por búsqueda.");

        var organizationId = currentUser.OrganizationId!.Value;

        var criteria = new ApifySearchCriteria(keywords, localities, request.MaxResults);
        var places = await apifyGoogleMapsClient.SearchAsync(criteria, ct);
        if (places.Count == 0)
            return Result<ImportPreviewDto>.Failure("Apify (Google Maps) no devolvió resultados (o la búsqueda falló).");

        var rows = places.Select(p => new ProspectCsvRow
        {
            business_name = p.Name,
            phone = p.PhoneNumber,
            whatsapp = WhatsAppCapableNumber(p.PhoneNumber),
            address = p.Address,
            city = p.City,
            province = p.Province,
            source = "apify"
        }).ToList();

        var batch = await BuildBatchAsync(
            rows, $"apify: {string.Join(", ", keywords)} — {string.Join(", ", localities)}", ProspectSourceType.ExternalApi, organizationId, ct);

        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        return Result<ImportPreviewDto>.Success(ToPreviewDto(batch));
    }

    // Devuelve el número ya corregido con el "9" insertado cuando hace falta (ver
    // AssumeWhatsAppCapable), listo para guardarse directo como contacto de WhatsApp.
    private static string? WhatsAppCapableNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var normalized = ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, phone);
        return ArgentineMobileDetector.AssumeWhatsAppCapable(normalized);
    }

    public async Task<Result<ImportPreviewDto>> GetPreviewAsync(int batchId, CancellationToken ct = default)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        return batch is null
            ? Result<ImportPreviewDto>.Failure("Importación no encontrada.")
            : Result<ImportPreviewDto>.Success(ToPreviewDto(batch));
    }

    public async Task<Result<IReadOnlyCollection<ImportRecordDto>>> GetRecordsAsync(int batchId, CancellationToken ct = default)
    {
        var batch = await db.ImportBatches.Include(b => b.Records).FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<IReadOnlyCollection<ImportRecordDto>>.Failure("Importación no encontrada.");

        var records = batch.Records.OrderBy(r => r.RowNumber).Select(ToRecordDto).ToList();
        return Result<IReadOnlyCollection<ImportRecordDto>>.Success(records);
    }

    // Filas Valid/Duplicate tienen NormalizedData (ya pasaron ContactValueNormalizer y el
    // parseo de categoría); filas Invalid nunca lo tienen, así que se cae al RawData crudo del
    // CSV/fuente externa para al menos mostrar lo que se intentó importar y por qué falló.
    private static ImportRecordDto ToRecordDto(ImportBatchRecord record)
    {
        if (record.NormalizedData is not null)
        {
            var normalized = JsonSerializer.Deserialize<NormalizedRow>(record.NormalizedData)!;
            var phone = normalized.Contacts.FirstOrDefault(c => c.Channel == ProspectContactChannel.Phone)?.Value;
            var whatsapp = normalized.Contacts.FirstOrDefault(c => c.Channel == ProspectContactChannel.Whatsapp)?.Value;

            return new ImportRecordDto(
                record.Id, record.RowNumber, record.Status.ToString(), normalized.BusinessName, normalized.Category.ToString(),
                phone, whatsapp, normalized.Address, normalized.City, normalized.Province, record.ErrorMessage);
        }

        var raw = JsonSerializer.Deserialize<ProspectCsvRow>(record.RawData)!;
        return new ImportRecordDto(
            record.Id, record.RowNumber, record.Status.ToString(), raw.business_name, raw.category,
            raw.phone, raw.whatsapp, raw.address, raw.city, raw.province, record.ErrorMessage);
    }

    public async Task<Result<ImportConfirmResultDto>> ConfirmAsync(int batchId, ConfirmImportRequest? request = null, CancellationToken ct = default)
    {
        var batch = await db.ImportBatches
            .Include(b => b.Records)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);

        if (batch is null)
            return Result<ImportConfirmResultDto>.Failure("Importación no encontrada.");

        if (batch.Status != ImportBatchStatus.Preview)
            return Result<ImportConfirmResultDto>.Failure($"La importación está en estado {batch.Status}, no se puede confirmar.");

        var selectedIds = request?.SelectedRecordIds;
        var created = 0;

        // ProspectDuplicateFinder (usado en el preview) sólo compara contra lo que ya está
        // persistido en la DB, no contra otras filas del mismo batch todavía sin guardar.
        // Dos filas "Valid" del mismo import pueden normalizar al mismo (Channel, Value)
        // (ej. la misma sucursal aparece dos veces en una búsqueda por overlap de radios), y
        // el índice único de ProspectContact rechazaría el segundo insert. Este set trackea
        // los valores ya usados en esta corrida para saltear ese contacto puntual en vez de
        // que todo el batch termine en un DbUpdateException.
        var contactValuesInBatch = new HashSet<(ProspectContactChannel Channel, string Value)>();

        // selectedIds == null preserva el comportamiento de siempre (importar todos los Valid):
        // así CSV y Google Places, que no mandan este parámetro, no cambian de comportamiento.
        foreach (var record in batch.Records.Where(r => r.Status == ImportBatchRecordStatus.Valid && (selectedIds is null || selectedIds.Contains(r.Id))))
        {
            var normalized = JsonSerializer.Deserialize<NormalizedRow>(record.NormalizedData!)!;

            var prospect = new Prospect
            {
                OrganizationId = batch.OrganizationId,
                BusinessName = normalized.BusinessName,
                Category = normalized.Category,
                Address = normalized.Address,
                City = normalized.City,
                Province = normalized.Province
            };

            foreach (var contact in normalized.Contacts)
            {
                if (!contactValuesInBatch.Add((contact.Channel, contact.Value)))
                    continue;

                prospect.Contacts.Add(new ProspectContact
                {
                    OrganizationId = batch.OrganizationId,
                    ProspectId = prospect.Id,
                    Prospect = prospect,
                    Channel = contact.Channel,
                    Value = contact.Value,
                    IsPrimary = contact.IsPrimary
                });
            }

            prospect.Sources.Add(new ProspectSource
            {
                OrganizationId = batch.OrganizationId,
                ProspectId = prospect.Id,
                Prospect = prospect,
                SourceType = batch.SourceType,
                SourceUrl = batch.FileName
            });

            db.Prospects.Add(prospect);
            record.ProspectId = prospect.Id;
            record.Status = ImportBatchRecordStatus.Imported;
            created++;
        }

        batch.Status = ImportBatchStatus.Completed;
        batch.CompletedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            return Result<ImportConfirmResultDto>.Failure(
                $"No se pudo confirmar la importación: uno o más contactos ya existen para la organización. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }

        return Result<ImportConfirmResultDto>.Success(new ImportConfirmResultDto(batch.Id, batch.Status.ToString(), created));
    }

    public async Task<Result<bool>> CancelAsync(int batchId, CancellationToken ct = default)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<bool>.Failure("Importación no encontrada.");

        if (batch.Status != ImportBatchStatus.Preview)
            return Result<bool>.Failure($"La importación está en estado {batch.Status}, no se puede cancelar.");

        batch.Status = ImportBatchStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    private async Task<ImportBatch> BuildBatchAsync(
        List<ProspectCsvRow> rows, string fileName, ProspectSourceType sourceType, int organizationId, CancellationToken ct)
    {
        var batch = new ImportBatch
        {
            OrganizationId = organizationId,
            FileName = fileName,
            SourceType = sourceType,
            CreatedBy = currentUser.UserId,
            Status = ImportBatchStatus.Preview,
            TotalRecords = rows.Count
        };

        var rowNumber = 0;
        foreach (var row in rows)
        {
            rowNumber++;
            batch.Records.Add(await BuildRecordAsync(batch, rowNumber, row, organizationId, ct));
        }

        batch.ValidRecords = batch.Records.Count(r => r.Status == ImportBatchRecordStatus.Valid);
        batch.DuplicateRecords = batch.Records.Count(r => r.Status == ImportBatchRecordStatus.Duplicate);
        batch.InvalidRecords = batch.Records.Count(r => r.Status == ImportBatchRecordStatus.Invalid);

        return batch;
    }

    private async Task<ImportBatchRecord> BuildRecordAsync(
        ImportBatch batch, int rowNumber, ProspectCsvRow row, int organizationId, CancellationToken ct)
    {
        var rawJson = JsonSerializer.Serialize(row);

        var businessName = row.business_name?.Trim();
        if (string.IsNullOrWhiteSpace(businessName))
        {
            return new ImportBatchRecord
            {
                ImportBatchId = batch.Id,
                ImportBatch = batch,
                RowNumber = rowNumber,
                RawData = rawJson,
                Status = ImportBatchRecordStatus.Invalid,
                ErrorMessage = "business_name es obligatorio."
            };
        }

        // Los valores se filtran por IsNullOrWhiteSpace *después* de normalizar: un teléfono
        // basura como "N/A" o "-" no tiene dígitos y ContactValueNormalizer.NormalizePhone
        // lo colapsa a "" (ver ContactValueNormalizer.cs). Si se dejara pasar, varias filas
        // del mismo import terminarían con el mismo (OrganizationId, Channel, "") y violarían
        // el índice único de ProspectContact al confirmar el batch.
        var contacts = new List<ContactInput>();
        var normalizedPhone = string.IsNullOrWhiteSpace(row.phone) ? null : ContactValueNormalizer.Normalize(ProspectContactChannel.Phone, row.phone);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
            contacts.Add(new ContactInput(ProspectContactChannel.Phone, normalizedPhone));
        var normalizedWhatsapp = string.IsNullOrWhiteSpace(row.whatsapp) ? null : ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, row.whatsapp);
        if (!string.IsNullOrWhiteSpace(normalizedWhatsapp))
            contacts.Add(new ContactInput(ProspectContactChannel.Whatsapp, normalizedWhatsapp));
        var normalizedEmail = string.IsNullOrWhiteSpace(row.email) ? null : ContactValueNormalizer.Normalize(ProspectContactChannel.Email, row.email);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
            contacts.Add(new ContactInput(ProspectContactChannel.Email, normalizedEmail));

        if (contacts.Count == 0)
        {
            return new ImportBatchRecord
            {
                ImportBatchId = batch.Id,
                ImportBatch = batch,
                RowNumber = rowNumber,
                RawData = rawJson,
                Status = ImportBatchRecordStatus.Invalid,
                ErrorMessage = "Debe tener al menos un contacto (phone, whatsapp o email)."
            };
        }

        contacts[0] = contacts[0] with { IsPrimary = true };

        var duplicateId = await duplicateFinder.FindDuplicateProspectIdAsync(organizationId, contacts, businessName, row.city, ct);
        if (duplicateId is not null)
        {
            return new ImportBatchRecord
            {
                ImportBatchId = batch.Id,
                ImportBatch = batch,
                RowNumber = rowNumber,
                RawData = rawJson,
                Status = ImportBatchRecordStatus.Duplicate,
                ProspectId = duplicateId,
                ErrorMessage = "Ya existe un prospecto con el mismo contacto."
            };
        }

        var category = Enum.TryParse<ProspectCategory>(row.category, ignoreCase: true, out var parsedCategory)
            ? parsedCategory
            : ProspectCategory.Unknown;

        var normalized = new NormalizedRow(businessName, category, row.address?.Trim(), row.city?.Trim(), row.province?.Trim(), contacts);

        return new ImportBatchRecord
        {
            ImportBatchId = batch.Id,
            ImportBatch = batch,
            RowNumber = rowNumber,
            RawData = rawJson,
            NormalizedData = JsonSerializer.Serialize(normalized),
            Status = ImportBatchRecordStatus.Valid
        };
    }

    private static ImportPreviewDto ToPreviewDto(ImportBatch batch) => new(
        batch.Id, batch.Status.ToString(), batch.TotalRecords, batch.ValidRecords, batch.DuplicateRecords, batch.InvalidRecords);

    private sealed record NormalizedRow(string BusinessName, ProspectCategory Category, string? Address, string? City, string? Province, List<ContactInput> Contacts);
}
