# HollowPoint.Azure

An Azure storage table wrapper - featuring a Generic class that allows you to query you Azure Table Storage Entities.

## Getting Started

Add a the HollowPoint.Azure assembly as a reference to you project.

## Linq to OData Example:

Where "People" is a simple "Microsoft.WindowsAzure.Storage.Table.TableEntity"

```
HollowPoint.Azure.DbTable<People> tt = new HollowPoint.Azure.DbTable<People>();

var filter = tt.Query()
.Where(t => t.Age >= 16 && t.Age < 33)
.ToODataString();   

```
OData Output\
```
((Age ge 16) and (Age lt 33))
```

## Linq - ToList() Example:

Retieve all items that match the below criteria:

```
HollowPoint.Azure.DbTable<People> tt = new HollowPoint.Azure.DbTable<People>();

var filter = tt.Query()
.Where(t => t.Age >= 16 && t.Age < 33)
.ToList();   

```

