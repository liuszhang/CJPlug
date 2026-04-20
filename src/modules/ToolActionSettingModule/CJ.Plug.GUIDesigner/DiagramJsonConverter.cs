using Blazor.Diagrams;
using Blazor.Diagrams.Core.Models;
using CJ.Plug.GUIDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.GUIDesigner
{
    public class DiagramJsonConverter : JsonConverter<BlazorDiagram>
    {

        private readonly string _name;

        public DiagramJsonConverter(string name)
        {
            _name = name;
        }


        public override BlazorDiagram Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 反序列化逻辑（根据需要实现）
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, BlazorDiagram value, JsonSerializerOptions options)
        {

            writer.WriteStartObject(); //开始自由容器
            writer.WriteString("type", "container");
            writer.WriteBoolean("isFreeContainer", true);
            writer.WriteString("size", "xs");
            writer.WriteBoolean("wrapperBody", false);
            writer.WritePropertyName("style");// 开始 style
            writer.WriteStartObject();
            writer.WriteString("position", "relative");
            writer.WriteString("minHeight", "200px");
            writer.WriteString("inset", "auto");
            writer.WriteEndObject();// 结束 style
            writer.WriteString("id", "u:2acb22f34f93");
            writer.WriteBoolean("isFixedHeight", false);
            writer.WriteBoolean("isFixedWidth", false);



            writer.WritePropertyName("body"); // 开始自由容器的内容数组
            JsonSerializer.Serialize(writer, ConvertNodes(value.Nodes), options);// 序列化节点数据到自由容器的 body 中

            writer.WriteEndObject();//结束自由容器



            // 添加可能需要的事件处理逻辑
            //writer.WritePropertyName("events");
            //writer.WriteStartObject();

            //// 例如，页面加载完成事件
            //writer.WritePropertyName("load");
            //writer.WriteStartObject();
            //writer.WritePropertyName("actions");
            //writer.WriteStartArray();
            //writer.WriteStartObject();
            //writer.WriteString("actionType", "custom");
            //writer.WriteString("script", "// 初始化流程图的 JavaScript 代码");
            //writer.WriteEndObject();
            //writer.WriteEndArray();
            //writer.WriteEndObject(); // load

            //writer.WriteEndObject(); // events

        }

        // 将 BlazorDiagram 节点转换为 AMIS 能使用的格式
        private List<Dictionary<string, object>> ConvertNodes(IEnumerable<NodeModel> nodes)
        {
            var result = new List<Dictionary<string, object>>();

            foreach (var node0 in nodes )
            {
                var node = node0 as BaseGuiItemModel;
                var nodeData1 = new Dictionary<string, object>
                {
                    ["id"] = node.Id,
                    ["type"] = node.Title ?? "default",
                    ["position"] = new { x = node.Position.X, y = node.Position.Y },
                    ["data"] = new { label = node.Title }
                };
                var nodeData=node.GuiItemService.ToAmisObject();
                var styleData = new
                {
                    position = "absolute",
                    inset = $"{node.Position.Y}px auto auto {node.Position.X}px",
                };
                // 序列化为JSON字节数组
                byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(nodeData);
                // 反序列化为Dictionary
                var nodeJson = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);


                // 直接添加对象属性，使用JsonNode来自动处理转移问题
                nodeJson["style"] = JsonNode.Parse(JsonSerializer.Serialize(styleData));

                result.Add(nodeJson);
            }

            return result;
        }
    }
}
