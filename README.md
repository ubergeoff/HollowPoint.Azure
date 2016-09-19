# HollowPoint.Azure

One Paragraph of project description goes here

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.


```
HollowPoint.Azure.DbTable<People> tt = new HollowPoint.Azure.DbTable<People>();


            var filter = tt.Query()
                .Where(t => t.Age >= 16 && t.Age < 33)
                .ToODataString();   
```


