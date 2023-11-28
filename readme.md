## Horizon Reports Custom Attribute Plugin Sample

These code samples show how you can use a custom function along with the "AfterLogin" event of an application plugin to create user-specific custom attribute fields. See the Horizon Reports [Plugin](https://developerdocs.horizon-reports.com/docs/plugins/index/) documentation for more details.

### SampleFunctions.cs

This file defines a custom function called GetAttributeValue. The function uses an "element ID" and an "attribute ID" to look up the value of a custom attribute field defined for that element. Attribute values are cached for a custom amount of time (5 minutes default). If a cache miss occurs, the values are retrieved from the database table "CustomAttribs". 

### SampleApplicationPlugin.cs

This file contains a sample application plugin, but only the "AfterLogin" event is implemented. When a user logs in, the AfterLogin method is called, and the custom attributes associated with this user's database are loaded. For each retrieved attribute, we create a calculated field in the "element" table that the attributes are associated with. The expression for each calculated field is a call to the custom "GetAttributeValue" function with the element ID of the "current row" (i.e. current row when a report is running). 