/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

14/06/2024	1.0.0.1		RSP, Skyline	Initial version
****************************************************************************
*/

namespace ConnectionTableDataSource_1
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net.Messages;

    [GQIMetaData(Name = "Core Connections")]
    public class ConnectionTable : IGQIDataSource, IGQIOnInit
    {
        private GQIDMS _dms;
        private StringBuilder _debugLogging;

        public GQIColumn[] GetColumns()
        {
            try
            {
                _debugLogging.AppendLine("GET COLUMNS");

                return new GQIColumn[]
                {
                    new GQIStringColumn("Source DMS Element Name"),
                    new GQIStringColumn("Source DMS Element Protocol"),
                    new GQIStringColumn("Source KEA Element Location ID"),
                    new GQIStringColumn("Source KEA Element Location Name"),
                    new GQIStringColumn("Source DMS DCF Interface Name"),
                    new GQIStringColumn("Destination DMS Element Name"),
                    new GQIStringColumn("Destination DMS Element Protocol"),
                    new GQIStringColumn("Destination KEA Element Location ID"),
                    new GQIStringColumn("Destination KEA Element Location Name"),
                    new GQIStringColumn("Destination DMS DCF Interface Name"),
                    new GQIStringColumn("Source DMS DCF Interface Table Index"),
                    new GQIStringColumn("Source DMS Element ID"),
                    new GQIStringColumn("Source DMS DCF Interface ID"),
                    new GQIStringColumn("Destination DMS DCF Interface ID"),
                    new GQIStringColumn("Source KEA Interface ID"),
                    new GQIStringColumn("Destination KEA Interface ID"),
                };
            }
            catch (Exception e)
            {
                _debugLogging.AppendLine("ERROR: " + e);
            }

            return null;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            try
            {
                _debugLogging.AppendLine("START|");
                string platformEndpointString = GetPlatformEndpointString();
                _debugLogging.AppendLine("platformEndpointString|" + platformEndpointString);
                List<string> interfacesInPlatformEndpoint = GetInterfacesFromPlatformEndpoint(platformEndpointString);
                _debugLogging.AppendLine("interfacesInPlatformEndpoint|" + String.Join(", ", interfacesInPlatformEndpoint));
                GQIPage page = new GQIPage(GetFilteredConnectionTableRows(interfacesInPlatformEndpoint));
                WriteToDebug(_debugLogging.ToString());
                return page;
            }
            catch (Exception e)
            {
                _debugLogging.AppendLine("ERROR: " + e);
                WriteToDebug(_debugLogging.ToString());
            }

            List<GQIRow> rows = new List<GQIRow>();
            return new GQIPage(rows.ToArray());
        }

        public void WriteToDebug(string text)
        {
            using (StreamWriter sw = new StreamWriter(@"C:\Skyline_Data\Debug\Debug.txt"))
            {
                sw.WriteLine(text);
            }
        }

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            try
            {
                _debugLogging = new StringBuilder();
                _dms = args.DMS;
                _debugLogging.AppendLine("INIT DONE");
            }
            catch (Exception e)
            {
                _debugLogging.AppendLine("ERROR: " + e);
            }

            return default;
        }

        public string GetPlatformEndpointString()
        {
            GetElementByNameMessage getElementByNameRequest = new GetElementByNameMessage("Kordia NPC Manager - Platform");
            IEnumerable<ElementInfoEventMessage> getElementByNameResponse = _dms.SendMessages(getElementByNameRequest).OfType<ElementInfoEventMessage>();

            if (!getElementByNameResponse.Any())
            {
                _debugLogging.AppendLine("PLATFORM MANAGER NOT FOUND");
                return null;
            }

            ElementInfoEventMessage platformNpcManager = getElementByNameResponse.First();
            return GetEndpointFromPlatformManager(platformNpcManager);
        }

        public string GetEndpointFromPlatformManager(ElementInfoEventMessage platformNpcManager)
        {
            GetParameterMessage getParameterRequest = new GetParameterMessage
            {
                DataMinerID = platformNpcManager.DataMinerID,
                ElId = platformNpcManager.ElementID,
                ParameterId = 21,
            };

            IEnumerable<GetParameterResponseMessage> getParameterResponse = _dms.SendMessages(getParameterRequest).OfType<GetParameterResponseMessage>();

            if (!getParameterResponse.Any())
            {
                _debugLogging.AppendLine("PLATFORM ENDPOINT NOT FOUND");
                return null;
            }

            ParameterValue paramValue = getParameterResponse.First().Value;

            return paramValue.ValueType == ParameterValueType.String ? paramValue.StringValue : null;
        }

        public List<string> GetInterfacesFromPlatformEndpoint(string platformEndpoint)
        {
            Dictionary<string, List<int>> platformDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(platformEndpoint);

            if (!platformDictionary.ContainsKey("Core"))
            {
                return new List<string>();
            }

            return platformDictionary["Core"].Select(id => id.ToString()).ToList();
        }

        public GQIRow[] GetFilteredConnectionTableRows(List<string> connectionInterfaceFilter)
        {
            GetElementByNameMessage getElementByNameRequest = new GetElementByNameMessage("Kordia NPC Manager - Connection");
            IEnumerable<ElementInfoEventMessage> getElementByNameResponse = _dms.SendMessages(getElementByNameRequest).OfType<ElementInfoEventMessage>();

            if (!getElementByNameResponse.Any())
            {
                _debugLogging.AppendLine("CONNECTION MANAGER NOT FOUND");
                return null;
            }

            ElementInfoEventMessage connectionManager = getElementByNameResponse.First();
            object[] connectionTableObject = GetConnectionTableObjectFromConnectionManager(connectionManager);
            return FilterAndConvertConnectionTableToRows(connectionTableObject, connectionInterfaceFilter);
        }

        private GQIRow[] FilterAndConvertConnectionTableToRows(object[] connectionTableObject, List<string> connectionInterfaceFilter)
        {
            List<GQIRow> rows = new List<GQIRow>();

            if (connectionTableObject.Length != 23)
            {
                _debugLogging.AppendLine("CONNECTION TABLE COLUMN COUND IS NOT 23");
                return null;
            }

            object[] instanceColumn = (object[])connectionTableObject[0];
            object[] srcKeaElementIdColumn = (object[])connectionTableObject[1];
            object[] srcKeaInterfaceIdColumn = (object[])connectionTableObject[2];
            object[] srcDmsElementIdColumn = (object[])connectionTableObject[3];
            object[] srcDmsElementNameColumn = (object[])connectionTableObject[4];
            object[] srcDmsElementProtocolColumn = (object[])connectionTableObject[5];
            object[] srcKeaElementLocationIdColumn = (object[])connectionTableObject[6];
            object[] srcKeaElementLocationNameColumn = (object[])connectionTableObject[7];
            object[] srcDmsDcfInterfaceIdColumn = (object[])connectionTableObject[8];
            object[] srcDmsDcfInterfaceNameColumn = (object[])connectionTableObject[9];
            object[] dstKeaElementIdColumn = (object[])connectionTableObject[10];
            object[] dstKeaInterfaceIdColumn = (object[])connectionTableObject[11];
            object[] dstDmsElementIdColumn = (object[])connectionTableObject[12];
            object[] dstDmsElementNameColumn = (object[])connectionTableObject[13];
            object[] dstDmsElementProtocolColumn = (object[])connectionTableObject[14];
            object[] dstKeaElementLocationIdColumn = (object[])connectionTableObject[15];
            object[] dstKeaElementLocationNameColumn = (object[])connectionTableObject[16];
            object[] dstDmsDcfInterfaceIdColumn = (object[])connectionTableObject[17];
            object[] dstDmsDcfInterfaceNameColumn = (object[])connectionTableObject[18];
            object[] srcDmsDcfInterfaceTableIdColumn = (object[])connectionTableObject[19];
            object[] srcDmsDcfInterfaceTableIndexColumn = (object[])connectionTableObject[20];
            object[] dstDmsDcfInterfaceTableIdColumn = (object[])connectionTableObject[21];
            object[] dstDmsDcfInterfaceTableIndexColumn = (object[])connectionTableObject[22];

            for (int i = 0; i < instanceColumn.Length; i++)
            {
                string srcKeaInterfaceId = Convert.ToString(((object[])srcKeaInterfaceIdColumn[i])[0]);
                string dstKeaInterfaceId = Convert.ToString(((object[])dstKeaInterfaceIdColumn[i])[0]);

                if (!connectionInterfaceFilter.Any() || (!connectionInterfaceFilter.Contains(srcKeaInterfaceId) && !connectionInterfaceFilter.Contains(dstKeaInterfaceId)))
                {
                    _debugLogging.AppendLine("FILTER DOES NOT CONTAIN " + srcKeaInterfaceId + " OR " + dstKeaInterfaceId);
                    continue;
                }


                _debugLogging.AppendLine("ADD CONNECTION " + srcKeaInterfaceId + " -> " + dstKeaInterfaceId);
                rows.Add(
                    new GQIRow(
                        new[]
                        {
                            new GQICell { Value = ((object[])srcDmsElementNameColumn[i])[0] },
                            new GQICell { Value = ((object[])srcDmsElementProtocolColumn[i])[0] },
                            new GQICell { Value = ((object[])srcKeaElementLocationIdColumn[i])[0] },
                            new GQICell { Value = ((object[])srcKeaElementLocationNameColumn[i])[0] },
                            new GQICell { Value = ((object[])srcDmsDcfInterfaceNameColumn[i])[0] },
                            new GQICell { Value = ((object[])dstDmsElementNameColumn[i])[0] },
                            new GQICell { Value = ((object[])dstDmsElementProtocolColumn[i])[0] },
                            new GQICell { Value = ((object[])dstKeaElementLocationIdColumn[i])[0] },
                            new GQICell { Value = ((object[])dstKeaElementLocationNameColumn[i])[0] },
                            new GQICell { Value = ((object[])dstDmsDcfInterfaceNameColumn[i])[0] },
                            new GQICell { Value = ((object[])srcDmsDcfInterfaceTableIndexColumn[i])[0] },
                            new GQICell { Value = ((object[])srcDmsElementIdColumn[i])[0] },
                            new GQICell { Value = ((object[])srcDmsDcfInterfaceIdColumn[i])[0] },
                            new GQICell { Value = ((object[])dstDmsDcfInterfaceIdColumn[i])[0] },
                            new GQICell { Value = srcKeaInterfaceId },
                            new GQICell { Value = dstKeaInterfaceId },
                        }));
            }


            _debugLogging.AppendLine("TOTAL ROWS: " + rows.Count);
            return rows.ToArray();
        }

        private object[] GetConnectionTableObjectFromConnectionManager(ElementInfoEventMessage connectionNpcManager)
        {
            GetPartialTableMessage getPartialTableRequest = new GetPartialTableMessage(connectionNpcManager.DataMinerID, connectionNpcManager.ElementID, 6000, Array.Empty<string>());
            IEnumerable<ParameterChangeEventMessage> getPartialTableResponse = _dms.SendMessages(getPartialTableRequest).OfType<ParameterChangeEventMessage>();

            if (!getPartialTableResponse.Any())
            {
                _debugLogging.AppendLine("CONNECTION TABLE NOT FOUND");
                return null;
            }

            ParameterChangeEventMessage connectionTableResponse = getPartialTableResponse.First();

            return (object[])connectionTableResponse.NewValue.InteropValue;
        }
    }
}