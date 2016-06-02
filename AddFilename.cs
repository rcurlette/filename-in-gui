using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using FilenameInGui;
using Tridion.ContentManager;
using Tridion.ContentManager.CoreService.Client;
using Tridion.Web.UI.Core.Extensibility;

// code samples used:
// http://sdllivecontent.sdl.com/LiveContent/web/pub.xql?action=home&pub=SDL_Tridion_2011_SPONE&lang=en-US#addHistory=true&filename=AddingANewColumnToAListView.xml&docid=task_E4EFBE6E5CA24C01B2531FB15AE95AE2&inner_id=&tid=&query=&scope=&resource=&eventType=lcContent.loadDoctask_E4EFBE6E5CA24C01B2531FB15AE95AE2
// http://www.sdltridionworld.com/community/2011_extensions/parentchangenotifier.aspx
// http://jaimesantosalcon.blogspot.com/2012/04/sdl-tridion-2011-data-extenders-real.html

// Deployment Instructions
// 1. Build project, copy DLLs to 'C:\Tridion\web\WebUI\WebRoot\bin'
// 2. Create folder 'AddFilename' for GUI Extension config file in 'C:\Tridion\web\WebUI\Editors' 
// 3. Create GUI Configuration File.  See example in this project.
// 4. Update Tridion System.config to enable GUI Extension, 'C:\Tridion\web\WebUI\WebRoot\Configuration'
// ie system.config
// <editor name="AddFilename">
//  <installpath>C:\Tridion\web\WebUI\WebRoot\Editors\AddFilename</installpath>
//  <configuration>AddFilename.config</configuration>
//  <vdir/>
//</editor>

//Implementation:
//Create Structure Group Metadata field 'DisableFilenameColumn'
//Checkbox Value:
//- 'yes'

//If yes, then no filenames will be shown and each structure group will not be opened by the extension to check the filename.  This will make things faster.
namespace GuiExtensions.DataExtenders
{
    public class FilenameInGui : DataExtender
    {
        static XmlDocument _xmlDoc;
        CoreService coreServiceClient = new CoreService();
        CoreServiceClient client = null; 

        public override string Name
        {
            get
            {
                Type itsMe = this.GetType();
                return String.Concat(itsMe.Namespace, ".", itsMe.Name);
            }
        }

        public override XmlTextReader ProcessRequest(XmlTextReader reader, PipelineContext context)
        {
            return reader;
        }

        public override XmlTextReader ProcessResponse(XmlTextReader reader, PipelineContext context)
        {
           
            client = coreServiceClient.GetClient();
            XmlTextReader xReader = reader;
            string command = context.Parameters["command"] as String;
            if (command == "GetList") // Code runs on every GetList
            {
                try
                {
                    Trace.Write("==========================Start PreprocessListItems " + System.DateTime.Now.ToShortDateString() + ", " + System.DateTime.Now.ToLongTimeString() + Environment.NewLine);
                    if (IsStructureGroupValid(context))
                    {
                        xReader = PreprocessListItems(reader, context);
                    }
                    Trace.Write("==========================Stop PreprocessListItems " + System.DateTime.Now.ToShortDateString() + ", " + System.DateTime.Now.ToLongTimeString() + Environment.NewLine);
                }
                catch
                { }
            }
            client.Close();
            return xReader;
        }

        private bool IsFilenameColumnDisabled(string uri)
        {
            bool IsFilenameColumnDisabled = false;
            string metaFieldname = "DisableFilenameColumn";
            StructureGroupData sgData = (StructureGroupData)client.Read(uri, new ReadOptions());
            if (sgData.Metadata != "")
            {
              var schemaFields = client.ReadSchemaFields(sgData.MetadataSchema.IdRef, true, new ReadOptions());
              var sgMetaFields = Fields.ForMetadataOf(schemaFields, sgData);
              if (sgMetaFields[metaFieldname] != null)
              {
                if (sgMetaFields[metaFieldname].Value == "yes")
                {
                  IsFilenameColumnDisabled = true;
                }
              }
            }
            return IsFilenameColumnDisabled;
        }

        private bool IsStructureGroupValid(PipelineContext context)
        {
            bool isStructureGroupValid = true;
            if (context.Parameters.ContainsKey("id"))
            {
                if (context.Parameters["id"].ToString().EndsWith("-4"))
                {
                    if (IsFilenameColumnDisabled(context.Parameters["id"].ToString()))
                    {
                        isStructureGroupValid = false;
                    }
                }
            }
            return isStructureGroupValid;
        }

        /// <summary>
        /// Idea here is to re-create the XmlTextReader Node and this accounts for 50% of the code.
        /// Original code borrowed from http://www.sdltridionworld.com/community/2011_extensions/parentchangenotifier.aspx
        /// Thanks for the work from Serguei Martchenko - would not be possible without his example!
        /// </summary>
        /// <param name="xReader"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private XmlTextReader PreprocessListItems(XmlTextReader xReader, PipelineContext context)
        {
            TextWriter sWriter = new StringWriter();
            XmlTextWriter xWriter = new XmlTextWriter(sWriter);
            string attrName = "pageFilename";
            string attrValue = "";  // set this to 'fieldValue', for example, to debug and prove it is working

            xReader.MoveToContent();

            while (!xReader.EOF)
            {
                switch (xReader.NodeType)
                {
                    case XmlNodeType.Element:
                        xWriter.WriteStartElement(xReader.Prefix, xReader.LocalName, xReader.NamespaceURI);

                        // add all attributes back  -- always START with this to NOT break the GUI
                        xWriter.WriteAttributes(xReader, false);

                        try
                        {
                            // add my custom attribute
                            if (IsValidItem(xReader))
                            {
                                string id = xReader.GetAttribute("ID");  // URI
                                string title = xReader.GetAttribute("Title");  // Title
                                string type = xReader.GetAttribute("Type"); //Type

                                if (type == "64")
                                {
                                    PageData pageData = (PageData)client.Read(id, new ReadOptions());
                                    attrValue = pageData.FileName;
                                }

                                // add new metadata field attribute
                                xWriter.WriteAttributeString(attrName, attrValue);
                                xReader.MoveToElement();
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("EXCEPTION " + ex.Message + ex.ToString() + ex.StackTrace);
                        }

                        if (xReader.IsEmptyElement)
                        {
                            xWriter.WriteEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        xWriter.WriteEndElement();
                        break;
                    case XmlNodeType.CDATA:
                        // Copy CDATA node  <![CDATA[]]>
                        xWriter.WriteCData(xReader.Value);
                        break;
                    case XmlNodeType.Comment:
                        // Copy comment node <!-- -->
                        xWriter.WriteComment(xReader.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        // Copy XML documenttype
                        xWriter.WriteDocType(xReader.Name, null, null, null);
                        break;
                    case XmlNodeType.EntityReference:
                        xWriter.WriteEntityRef(xReader.Name);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        xWriter.WriteProcessingInstruction(xReader.Name, xReader.Value);
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        xWriter.WriteWhitespace(xReader.Value);
                        break;
                    case XmlNodeType.Text:
                        xWriter.WriteString(xReader.Value);
                        break;
                    case XmlNodeType.Whitespace:
                        xWriter.WriteWhitespace(xReader.Value);
                        break;
                }
                xReader.Read();
            };
            
            xWriter.Flush();

            xReader = new XmlTextReader(new StringReader(sWriter.ToString()));
            xReader.MoveToContent();
            //-> Write XML of tcm:Item out...
            //   This is where the attribute in the config file is matched.  
            Trace.Write(sWriter.ToString() + Environment.NewLine);
            return xReader;
        }

        
        /// <summary>
        /// Get Xml Doc
        /// </summary>
        /// <returns>XmlDocument</returns>
        private static XmlDocument GetXmlDoc()
        {
            if (_xmlDoc == null)
            {
                XmlDocument xmlDoc = new XmlDocument();
                _xmlDoc = xmlDoc;
            }
            return _xmlDoc;
        }

       
        /// <summary>
        /// Check if an item node 
        /// </summary>
        /// <param name="xReader"></param>
        /// <returns>True if we have an Item node</returns>
        private bool IsValidItem(XmlTextReader xReader)
        {
            if (xReader.LocalName == "Item")// && xReader.NamespaceURI == TDSDefines.Constants.NS_DS)
                return true;
            else
                return false;
        }
    }
}