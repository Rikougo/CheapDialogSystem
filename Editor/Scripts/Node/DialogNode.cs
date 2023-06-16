using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace CheapDialogSystem.Editor.Node
{
    public class DialogNode : UnityEditor.Experimental.GraphView.Node
    {
        public string DialogText;
        public string GUID;
        public bool EntryPoint = false;

        public Action<Edge> PortSuppressed; 

        public void OnTextChangeEvent(ChangeEvent<string> p_event)
        {
            DialogText = p_event.newValue;
        }

        public void AddChoicePort()
        {
            this.AddChoicePort("New choice");
        }
        
        public void AddChoicePort(string p_portName)
        {
            var l_generatedPort = this.GetPortInstance(Direction.Output);
            var l_portLabel = l_generatedPort.contentContainer.Q<Label>("type");
            l_generatedPort.contentContainer.Remove(l_portLabel);


            TextField l_textField = new TextField()
            {
                name = string.Empty,
                value = p_portName
            };
            l_textField.RegisterValueChangedCallback(p_event => l_generatedPort.portName = p_event.newValue);
            l_generatedPort.contentContainer.Add(new Label("  "));
            l_generatedPort.contentContainer.Add(l_textField);
            Button l_deleteButton = new Button(() => this.RemovePort(l_generatedPort))
            {
                text = "X"
            };
            l_generatedPort.contentContainer.Add(l_deleteButton);
            l_generatedPort.portName = p_portName;
            l_generatedPort.name = "output_port"; // uss name
            this.outputContainer.Add(l_generatedPort);
            this.RefreshPorts();
            this.RefreshExpandedState();
        }
        
        public Port GetPortInstance(Direction p_nodeDirection, Port.Capacity p_capacity = Port.Capacity.Single)
        {
            return this.InstantiatePort(Orientation.Horizontal, p_nodeDirection, p_capacity, typeof(float));
        }
        
        private void RemovePort(Port p_socket)
        {
            Edge[] l_targetEdge = p_socket.connections
                .Where(p_edge =>
                    p_edge.output.portName == p_socket.portName && p_edge.output.node == p_socket.node
                ).ToArray();
            if (l_targetEdge.Any())
            {
                Edge l_edge = l_targetEdge.First();
                l_edge.input.Disconnect(l_edge);
                l_edge.output.Disconnect(l_edge);
                
                PortSuppressed?.Invoke(l_edge);
            }

            this.outputContainer.Remove(p_socket);
            this.RefreshPorts();
            this.RefreshExpandedState();
        }
    }
}