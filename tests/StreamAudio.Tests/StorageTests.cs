using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using StreamAudio.Core.Storage;
using Xunit;

namespace StreamAudio.Tests;

[Collection("AudioTest")]
public class StorageTests : IDisposable
{
  private readonly string _testDir;
  private readonly List<IStorage> _storageInstances = new();

  public StorageTests()
  {
    _testDir = Path.Combine(Path.GetTempPath(), $"streamaudio_test_{Guid.NewGuid():N}");
    Directory.CreateDirectory(_testDir);
  }

  public void Dispose()
  {
    foreach (var storage in _storageInstances)
    {
      storage?.Dispose();
    }

    if (Directory.Exists(_testDir))
    {
      Directory.Delete(_testDir, true);
    }
  }

  private IStorage CreateJsonStorage()
  {
    var storageDir = Path.Combine(_testDir, "json_storage");
    var backupDir = Path.Combine(_testDir, "json_backup");
    var storage = new JsonFileStorage(storageDir, backupDir);
    _storageInstances.Add(storage);
    return storage;
  }

  private IStorage CreateSqliteStorage()
  {
    var dbPath = Path.Combine(_testDir, $"test_{Guid.NewGuid():N}.db");
    var backupDir = Path.Combine(_testDir, "sqlite_backup");
    var storage = new SqliteStorage(dbPath, backupDir);
    _storageInstances.Add(storage);
    return storage;
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task SaveAsync_Should_Store_Data(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    var testData = new TestModel { Id = 1, Name = "Test", Value = "TestValue" };

    // Act
    await storage.SaveAsync("TestTable", "key1", testData);

    // Assert
    var loaded = await storage.LoadAsync<TestModel>("TestTable", "key1");
    loaded.Should().NotBeNull();
    loaded!.Id.Should().Be(testData.Id);
    loaded.Name.Should().Be(testData.Name);
    loaded.Value.Should().Be(testData.Value);
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task LoadAsync_Should_Return_Null_For_NonExistent_Key(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();

    // Act
    var result = await storage.LoadAsync<TestModel>("TestTable", "nonexistent");

    // Assert
    result.Should().BeNull();
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task ExistsAsync_Should_Return_True_For_Existing_Key(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Test" });

    // Act
    var exists = await storage.ExistsAsync("TestTable", "key1");

    // Assert
    exists.Should().BeTrue();
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task ExistsAsync_Should_Return_False_For_NonExistent_Key(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();

    // Act
    var exists = await storage.ExistsAsync("TestTable", "nonexistent");

    // Assert
    exists.Should().BeFalse();
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task DeleteAsync_Should_Remove_Data(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Test" });

    // Act
    await storage.DeleteAsync("TestTable", "key1");

    // Assert
    var exists = await storage.ExistsAsync("TestTable", "key1");
    exists.Should().BeFalse();
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task GetKeysAsync_Should_Return_All_Keys(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Test1" });
    await storage.SaveAsync("TestTable", "key2", new TestModel { Id = 2, Name = "Test2" });
    await storage.SaveAsync("TestTable", "key3", new TestModel { Id = 3, Name = "Test3" });

    // Act
    var keys = (await storage.GetKeysAsync("TestTable")).ToList();

    // Assert
    keys.Should().HaveCount(3);
    keys.Should().Contain("key1");
    keys.Should().Contain("key2");
    keys.Should().Contain("key3");
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task GetAllAsync_Should_Return_All_Values(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Test1" });
    await storage.SaveAsync("TestTable", "key2", new TestModel { Id = 2, Name = "Test2" });
    await storage.SaveAsync("TestTable", "key3", new TestModel { Id = 3, Name = "Test3" });

    // Act
    var values = (await storage.GetAllAsync<TestModel>("TestTable")).ToList();

    // Assert
    values.Should().HaveCount(3);
    values.Should().Contain(v => v.Id == 1 && v.Name == "Test1");
    values.Should().Contain(v => v.Id == 2 && v.Name == "Test2");
    values.Should().Contain(v => v.Id == 3 && v.Name == "Test3");
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task GetTablesAsync_Should_Return_All_Tables(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("Table1", "key1", new TestModel { Id = 1 });
    await storage.SaveAsync("Table2", "key1", new TestModel { Id = 2 });
    await storage.SaveAsync("Table3", "key1", new TestModel { Id = 3 });

    // Act
    var tables = (await storage.GetTablesAsync()).ToList();

    // Assert
    tables.Should().Contain("Table1");
    tables.Should().Contain("Table2");
    tables.Should().Contain("Table3");
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task SaveAsync_Should_Overwrite_Existing_Data(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Original" });

    // Act
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Updated" });

    // Assert
    var loaded = await storage.LoadAsync<TestModel>("TestTable", "key1");
    loaded!.Name.Should().Be("Updated");
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task Secrets_Should_Be_Resolved_Automatically(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("SECRETS", "api_key", "super_secret_value");
    await storage.SaveAsync("Config", "api_config", new TestModel
    {
      Id = 1,
      Name = "API Config",
      Value = "[SECRET:api_key]"
    });

    // Act
    var loaded = await storage.LoadAsync<TestModel>("Config", "api_config");

    // Assert
    loaded.Should().NotBeNull();
    loaded!.Value.Should().Be("super_secret_value");
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task BackupAsync_Should_Create_Backup_File(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Test" });

    // Act
    var backupPath = await storage.BackupAsync();

    // Assert
    File.Exists(backupPath).Should().BeTrue();
  }

  [Theory]
  [InlineData("Json")]
  [InlineData("Sqlite")]
  public async Task RestoreAsync_Should_Restore_Data_From_Backup(string storageType)
  {
    // Arrange
    var storage = storageType == "Json" ? CreateJsonStorage() : CreateSqliteStorage();
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Original" });
    var backupPath = await storage.BackupAsync();

    // Modify data
    await storage.SaveAsync("TestTable", "key1", new TestModel { Id = 1, Name = "Modified" });

    // Act
    await storage.RestoreAsync(backupPath);

    // Assert
    var loaded = await storage.LoadAsync<TestModel>("TestTable", "key1");
    loaded!.Name.Should().Be("Original");
  }

  [Fact]
  public async Task ReplicateStorageAsync_Should_Copy_Data_From_Json_To_Sqlite()
  {
    // Arrange
    var jsonStorage = CreateJsonStorage();
    var sqliteStorage = CreateSqliteStorage();

    await jsonStorage.SaveAsync("Table1", "key1", new TestModel { Id = 1, Name = "Test1" });
    await jsonStorage.SaveAsync("Table1", "key2", new TestModel { Id = 2, Name = "Test2" });
    await jsonStorage.SaveAsync("Table2", "key3", new TestModel { Id = 3, Name = "Test3" });

    // Act
    await jsonStorage.ReplicateStorageAsync(sqliteStorage);

    // Assert
    var table1Keys = (await sqliteStorage.GetKeysAsync("Table1")).ToList();
    table1Keys.Should().HaveCount(2);

    var table2Keys = (await sqliteStorage.GetKeysAsync("Table2")).ToList();
    table2Keys.Should().HaveCount(1);

    var loaded = await sqliteStorage.LoadAsync<TestModel>("Table1", "key1");
    loaded!.Name.Should().Be("Test1");
  }

  [Fact]
  public async Task ReplicateStorageAsync_Should_Copy_Data_From_Sqlite_To_Json()
  {
    // Arrange
    var sqliteStorage = CreateSqliteStorage();
    var jsonStorage = CreateJsonStorage();

    await sqliteStorage.SaveAsync("Table1", "key1", new TestModel { Id = 1, Name = "Test1" });
    await sqliteStorage.SaveAsync("Table1", "key2", new TestModel { Id = 2, Name = "Test2" });
    await sqliteStorage.SaveAsync("Table2", "key3", new TestModel { Id = 3, Name = "Test3" });

    // Act
    await sqliteStorage.ReplicateStorageAsync(jsonStorage);

    // Assert
    var table1Keys = (await jsonStorage.GetKeysAsync("Table1")).ToList();
    table1Keys.Should().HaveCount(2);

    var table2Keys = (await jsonStorage.GetKeysAsync("Table2")).ToList();
    table2Keys.Should().HaveCount(1);

    var loaded = await jsonStorage.LoadAsync<TestModel>("Table1", "key1");
    loaded!.Name.Should().Be("Test1");
  }

  private class TestModel
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
  }
}
