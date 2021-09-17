namespace OpcPlc.Helpers
{
    using OpcPlc.PluginNodes.Models;
    using Serilog;
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class PnJsonHelper
    {
        /// <summary>
        /// Show and save pn.json
        /// </summary>
        public static async Task DumpPublisherConfigJsonAsync(string pnJsonFileName, string serverPath, ImmutableList<IPluginNodes> pluginNodes, ILogger logger)
        {
            var sb = new StringBuilder();

            sb.AppendLine(Environment.NewLine + "[");
            sb.AppendLine("  {");
            sb.AppendLine($"    \"EndpointUrl\": \"opc.tcp://{serverPath}\",");
            sb.AppendLine("    \"UseSecurity\": false,");
            sb.AppendLine("    \"OpcNodes\": [");

            // Print config from plugin nodes list.
            foreach (var plugin in pluginNodes)
            {
                foreach (var node in plugin.Nodes)
                {
                    // Show only if > 0 and != 1000 ms.
                    string publishingInterval = node.PublishingInterval > 0 &&
                                                node.PublishingInterval != 1000
                        ? $", \"OpcPublishingInterval\": {node.PublishingInterval}"
                        : string.Empty;
                    // Show only if > 0 ms.
                    string samplingInterval = node.SamplingInterval > 0
                        ? $", \"OpcSamplingInterval\": {node.SamplingInterval}"
                        : string.Empty;

                    string nodeId = JsonEncodedText.Encode(node.NodeId, JavaScriptEncoder.Default).ToString();
                    sb.AppendLine($"      {{ \"Id\": \"nsu={node.Namespace};s={nodeId}\"{publishingInterval}{samplingInterval} }},");
                }
            }

            int trimLen = Environment.NewLine.Length + 1;
            sb.Remove(sb.Length - trimLen, trimLen); // Trim trailing ,\n.

            sb.AppendLine(Environment.NewLine + "    ]");
            sb.AppendLine("  }");
            sb.AppendLine("]");

            string pnJson = sb.ToString();
            logger.Information($"OPC Publisher configuration file: {pnJsonFileName}" + pnJson);

            await File.WriteAllTextAsync(pnJsonFileName, pnJson.Trim()).ConfigureAwait(false);
        }
    }
}
