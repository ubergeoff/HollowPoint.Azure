# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

Build the solution:
```bash
msbuild HollowPoint.Azure.sln /p:Configuration=Debug
# or
msbuild HollowPoint.Azure.sln /p:Configuration=Release
```

Build specific project:
```bash
msbuild HollowPoint.Azure/HollowPoint.Azure.csproj /p:Configuration=Debug
```

## Project Overview

HollowPoint.Azure is a .NET Framework 4.8 library that provides a simplified abstraction layer for Azure Table Storage operations with LINQ query support.

### Key Components

**DbTable<T>** (DbTable.cs:17)
- Generic repository pattern implementation for Azure Table Storage
- Manages CloudTableClient and CloudTable lifecycle
- Provides CRUD operations (Add, Update, Delete, Get) with both sync and async variants
- Uses HttpContext.Current.Cache to cache table existence checks
- Table names are derived from the generic type parameter T

**BaseTableEntity** (BaseTableEntity.cs:9)
- Base class for all table entities in the library
- Extends Microsoft.WindowsAzure.Storage.Table.TableEntity
- Provides common fields: DateAdded, Title, IsDeleted
- All entities must have PartitionKey and RowKey (inherited from TableEntity)

**ODataTranslator** (Tools/ODataTranslator.cs:12)
- Expression visitor that converts LINQ expressions to OData filter strings
- Supports Where, Take, and Skip operations
- Translates C# comparison operators to OData equivalents (eq, ne, lt, le, gt, ge)
- Handles binary expressions (and, or) and unary expressions (not)

**QueryableExtension** (Extensions/QueryableExtension.cs:20)
- Extension method ToODataString<T>() that extracts OData query string from IQueryable
- Uses reflection to access internal query expression structure
- Delegates to ODataTranslator for expression translation

**CloudConfigurationManager** (Common/CloudConfigurationManager.cs:9)
- Configuration abstraction for Azure services
- Returns placeholder values for StorageConnectionString, AzureSearchApiKey, and AzureSearchServiceName
- Users must customize this class with their actual configuration values

## Architecture

This library follows a repository pattern where:
1. Entity classes inherit from BaseTableEntity
2. DbTable<T> provides typed access to Azure Table Storage
3. LINQ queries are automatically translated to OData filter expressions
4. Table existence is cached in HttpContext to minimize storage calls

The OData translation pipeline:
```
IQueryable<T> → Expression Tree → ODataTranslator → OData Filter String
```

## Configuration Required

Before using this library, modify CloudConfigurationManager.cs to return actual values instead of placeholder strings:
- StorageConnectionString: Azure Storage connection string
- AzureSearchApiKey: Azure Search API key (if using search features)
- AzureSearchServiceName: Azure Search service name (if using search features)

## Dependencies

- .NET Framework 4.8
- Microsoft.WindowsAzure.Storage 7.2.0
- Microsoft.Rest.ClientRuntime.Azure 3.3.2
- Newtonsoft.Json 6.0.8
- Requires HttpContext (System.Web) for caching functionality
