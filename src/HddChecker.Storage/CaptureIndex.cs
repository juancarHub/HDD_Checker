using System.Globalization;
using HddChecker.Core.Acquisition;
using Microsoft.Data.Sqlite;

namespace HddChecker.Storage;

public sealed class CaptureIndex(string databasePath)
{
    public string DatabasePath { get; } = Path.GetFullPath(databasePath);

    public async Task<CaptureIndexSummary> SynchronizeAsync(
        CaptureCatalog catalog,
        CancellationToken cancellationToken = default)
    {
        var parent = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        await using var connection = CreateConnection(SqliteOpenMode.ReadWriteCreate);
        await connection.OpenAsync(cancellationToken);
        await InitializeSchemaAsync(connection, cancellationToken);

        using var transaction = connection.BeginTransaction();
        foreach (var device in catalog.Devices)
        {
            await UpsertDeviceAsync(connection, transaction, device, cancellationToken);
            foreach (var capture in device.Captures)
            {
                await UpsertCaptureAsync(connection, transaction, capture, cancellationToken);
            }
        }

        transaction.Commit();
        return new CaptureIndexSummary(
            catalog.Devices.Count,
            catalog.Devices.Sum(device => device.Captures.Count),
            catalog.Issues.Count);
    }

    public async Task<(long Devices, long Captures)> GetCountsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection(SqliteOpenMode.ReadOnly);
        await connection.OpenAsync(cancellationToken);
        return (
            await ExecuteCountAsync(connection, "devices", cancellationToken),
            await ExecuteCountAsync(connection, "captures", cancellationToken));
    }

    private static async Task InitializeSchemaAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
        PRAGMA foreign_keys = ON;
        CREATE TABLE IF NOT EXISTS devices (
            identity_key TEXT PRIMARY KEY,
            identity_kind TEXT NOT NULL,
            identity_value TEXT NOT NULL,
            model_name TEXT,
            serial_number TEXT,
            wwn TEXT,
            protocol TEXT,
            capacity_bytes INTEGER,
            first_capture_utc TEXT NOT NULL,
            latest_capture_utc TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS captures (
            identity_key TEXT NOT NULL,
            capture_id TEXT NOT NULL,
            captured_at_utc TEXT NOT NULL,
            directory_path TEXT NOT NULL UNIQUE,
            schema_version INTEGER NOT NULL,
            acquisition_outcome TEXT NOT NULL,
            smartctl_exit_status TEXT NOT NULL,
            smart_passed INTEGER,
            power_on_hours INTEGER,
            power_cycle_count INTEGER,
            load_cycle_count INTEGER,
            temperature_celsius INTEGER,
            remaining_life_percent INTEGER,
            nvme_available_spare_percent INTEGER,
            nvme_data_units_read INTEGER,
            nvme_data_units_written INTEGER,
            nvme_unsafe_shutdowns INTEGER,
            nvme_media_errors INTEGER,
            nvme_error_log_entries INTEGER,
            reallocated_sectors INTEGER,
            pending_sectors INTEGER,
            offline_uncorrectable_sectors INTEGER,
            reported_uncorrectable_errors INTEGER,
            sata_crc_errors INTEGER,
            PRIMARY KEY (identity_key, capture_id),
            FOREIGN KEY (identity_key) REFERENCES devices(identity_key)
        );
        CREATE INDEX IF NOT EXISTS ix_captures_identity_time
            ON captures(identity_key, captured_at_utc);
        PRAGMA user_version = 1;
        """;
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertDeviceAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        DeviceCaptureHistory device,
        CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO devices (
            identity_key, identity_kind, identity_value, model_name, serial_number,
            wwn, protocol, capacity_bytes, first_capture_utc, latest_capture_utc)
        VALUES (
            $key, $kind, $value, $model, $serial,
            $wwn, $protocol, $capacity, $first, $latest)
        ON CONFLICT(identity_key) DO UPDATE SET
            model_name = excluded.model_name,
            serial_number = excluded.serial_number,
            wwn = excluded.wwn,
            protocol = excluded.protocol,
            capacity_bytes = excluded.capacity_bytes,
            first_capture_utc = MIN(devices.first_capture_utc, excluded.first_capture_utc),
            latest_capture_utc = MAX(devices.latest_capture_utc, excluded.latest_capture_utc);
        """;
        var snapshot = device.Latest.Package.Snapshot;
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        Add(command, "$key", device.Identity.Key);
        Add(command, "$kind", device.Identity.Kind);
        Add(command, "$value", device.Identity.Value);
        Add(command, "$model", snapshot.ModelName);
        Add(command, "$serial", snapshot.SerialNumber);
        Add(command, "$wwn", snapshot.WorldWideName);
        Add(command, "$protocol", snapshot.Protocol);
        Add(command, "$capacity", snapshot.CapacityBytes);
        Add(command, "$first", FormatTimestamp(device.First.Package.CapturedAtUtc));
        Add(command, "$latest", FormatTimestamp(device.Latest.Package.CapturedAtUtc));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertCaptureAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CaptureCatalogEntry capture,
        CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO captures (
            identity_key, capture_id, captured_at_utc, directory_path, schema_version,
            acquisition_outcome, smartctl_exit_status, smart_passed, power_on_hours,
            power_cycle_count, load_cycle_count, temperature_celsius,
            remaining_life_percent, nvme_available_spare_percent, nvme_data_units_read,
            nvme_data_units_written, nvme_unsafe_shutdowns, nvme_media_errors,
            nvme_error_log_entries, reallocated_sectors, pending_sectors,
            offline_uncorrectable_sectors, reported_uncorrectable_errors, sata_crc_errors)
        VALUES (
            $identity, $capture, $timestamp, $path, $schema,
            $outcome, $exit, $smart, $hours,
            $cycles, $loadCycles, $temperature,
            $life, $spare, $read,
            $written, $unsafe, $media,
            $errorLog, $reallocated, $pending,
            $uncorrectable, $reported, $crc)
        ON CONFLICT(identity_key, capture_id) DO UPDATE SET
            captured_at_utc = excluded.captured_at_utc,
            directory_path = excluded.directory_path,
            schema_version = excluded.schema_version,
            acquisition_outcome = excluded.acquisition_outcome,
            smartctl_exit_status = excluded.smartctl_exit_status,
            smart_passed = excluded.smart_passed,
            power_on_hours = excluded.power_on_hours,
            power_cycle_count = excluded.power_cycle_count,
            load_cycle_count = excluded.load_cycle_count,
            temperature_celsius = excluded.temperature_celsius,
            remaining_life_percent = excluded.remaining_life_percent,
            nvme_available_spare_percent = excluded.nvme_available_spare_percent,
            nvme_data_units_read = excluded.nvme_data_units_read,
            nvme_data_units_written = excluded.nvme_data_units_written,
            nvme_unsafe_shutdowns = excluded.nvme_unsafe_shutdowns,
            nvme_media_errors = excluded.nvme_media_errors,
            nvme_error_log_entries = excluded.nvme_error_log_entries,
            reallocated_sectors = excluded.reallocated_sectors,
            pending_sectors = excluded.pending_sectors,
            offline_uncorrectable_sectors = excluded.offline_uncorrectable_sectors,
            reported_uncorrectable_errors = excluded.reported_uncorrectable_errors,
            sata_crc_errors = excluded.sata_crc_errors;
        """;
        var package = capture.Package;
        var snapshot = package.Snapshot;
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        Add(command, "$identity", capture.Identity.Key);
        Add(command, "$capture", package.CaptureId);
        Add(command, "$timestamp", FormatTimestamp(package.CapturedAtUtc));
        Add(command, "$path", capture.DirectoryPath);
        Add(command, "$schema", package.SchemaVersion);
        Add(command, "$outcome", package.Assessment.Outcome.ToString());
        Add(command, "$exit", package.Assessment.ExitStatus.ToString());
        Add(command, "$smart", snapshot.SmartPassed is null ? null : snapshot.SmartPassed.Value ? 1 : 0);
        Add(command, "$hours", snapshot.PowerOnHours);
        Add(command, "$cycles", snapshot.PowerCycleCount);
        Add(command, "$loadCycles", snapshot.LoadCycleCount);
        Add(command, "$temperature", snapshot.TemperatureCelsius);
        Add(command, "$life", snapshot.RemainingLifePercent);
        Add(command, "$spare", snapshot.NvmeAvailableSparePercent);
        Add(command, "$read", snapshot.NvmeDataUnitsRead);
        Add(command, "$written", snapshot.NvmeDataUnitsWritten);
        Add(command, "$unsafe", snapshot.NvmeUnsafeShutdowns);
        Add(command, "$media", snapshot.NvmeMediaErrors);
        Add(command, "$errorLog", snapshot.NvmeErrorLogEntries);
        Add(command, "$reallocated", snapshot.ReallocatedSectors);
        Add(command, "$pending", snapshot.PendingSectors);
        Add(command, "$uncorrectable", snapshot.OfflineUncorrectableSectors);
        Add(command, "$reported", snapshot.ReportedUncorrectableErrors);
        Add(command, "$crc", snapshot.SataCrcErrors);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Add(SqliteCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static async Task<long> ExecuteCountAsync(
        SqliteConnection connection,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table};";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private SqliteConnection CreateConnection(SqliteOpenMode mode)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = mode,
            Pooling = false,
        };
        return new SqliteConnection(builder.ToString());
    }
}
