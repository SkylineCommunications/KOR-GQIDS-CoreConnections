# KOR-GQIDC-CoreConnections
This automation script implements a GQI Ad-Hoc Data Source based upon the following logic:

1. All data contained within 'KEA DCF Connections' table in the 'Kordia NPC Manager - Connection' element is retrieved.
1. This data is filtered using the list of interface IDs contained within the 'Core' list in the KEA Platforms endpoint. This list is retrieved from the Kordia NPC Interface element.
1. The resulting filtered data is returned to GQI. All columns in the table are returned.

In the event whereby:
- There is no 'Core' list in KEA Platform endpoint data, or
- 'Core' list is empty

The script will return full list of connections to reduce impact on Node-Edge end users. The is because it is understood that not all provisioned connections are 'Core' connections. So having a non-existent or empty 'Core' list is very likely an error in KEA.

The primary usage of this Ad-Hoc Data Source is to retrieve a filtered list of connections that will make up the Edges in the NodeEdge diagram for Kordia WeatherMaps.
