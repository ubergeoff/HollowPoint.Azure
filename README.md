# HollowPoint.Azure

One Paragraph of project description goes here

## Getting Started

Add a the HollowPoint.Azure assembly as a reference to you project.

## Example

```
HollowPoint.Azure.DbTable<People> tt = new HollowPoint.Azure.DbTable<People>();

var filter = tt.Query()
.Where(t => t.Age >= 16 && t.Age < 33)
.ToODataString();   

```
## OData Output

((Age ge 16) and (Age lt 33))

