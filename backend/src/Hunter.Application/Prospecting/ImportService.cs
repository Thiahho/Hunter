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
    IGooglePlacesClient googlePlacesClient) : IImportService
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

    public async Task<Result<ImportPreviewDto>> GetPreviewAsync(int batchId, CancellationToken ct = default)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        return batch is null
            ? Result<ImportPreviewDto>.Failure("Importación no encontrada.")
            : Result<ImportPreviewDto>.Success(ToPreviewDto(batch));
    }

    public async Task<Result<ImportConfirmResultDto>> ConfirmAsync(int batchId, CancellationToken ct = default)
    {
        var batch = await db.ImportBatches
            .Include(b => b.Records)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);

        if (batch is null)
            return Result<ImportConfirmResultDto>.Failure("Importación no encontrada.");

        if (batch.Status != ImportBatchStatus.Preview)
            return Result<ImportConfirmResultDto>.Failure($"La importación está en estado {batch.Status}, no se puede confirmar.");

        var created = 0;

        foreach (var record in batch.Records.Where(r => r.Status == ImportBatchRecordStatus.Valid))
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

        await db.SaveChangesAsync(ct);

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

        var contacts = new List<ContactInput>();
        if (!string.IsNullOrWhiteSpace(row.phone))
            contacts.Add(new ContactInput(ProspectContactChannel.Phone, ContactValueNormalizer.Normalize(ProspectContactChannel.Phone, row.phone)));
        if (!string.IsNullOrWhiteSpace(row.whatsapp))
            contacts.Add(new ContactInput(ProspectContactChannel.Whatsapp, ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, row.whatsapp)));
        if (!string.IsNullOrWhiteSpace(row.email))
            contacts.Add(new ContactInput(ProspectContactChannel.Email, ContactValueNormalizer.Normalize(ProspectContactChannel.Email, row.email)));

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

        var duplicateId = await duplicateFinder.FindDuplicateProspectIdAsync(organizationId, contacts, ct);
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
