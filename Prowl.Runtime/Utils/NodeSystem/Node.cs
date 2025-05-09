﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using Prowl.Echo;
using Prowl.Runtime.Utils;
using Prowl.Runtime.Utils.NodeSystem;

namespace Prowl.Runtime.NodeSystem;

/// <summary>
/// Base class for all nodes
/// </summary>
/// <example>
/// Classes extending this class will be considered as valid nodes by xNode.
/// <code>
/// [System.Serializable]
/// public class Adder : Node {
///     [Input] public float a;
///     [Input] public float b;
///     [Output] public float result;
///
///     // GetValue should be overridden to return a value for any specified output port
///     public override object GetValue(NodePort port) {
///         return a + b;
///     }
/// }
/// </code>
/// </example>
[Serializable]
public abstract class Node
{
    /// <summary> Used by <see cref="InputAttribute"/> and <see cref="OutputAttribute"/> to determine when to display the field value associated with a <see cref="NodePort"/> </summary>
    public enum ShowBackingValue
    {
        /// <summary> Never show the backing value </summary>
        Never,
        /// <summary> Show the backing value only when the port does not have any active connections </summary>
        Unconnected,
        /// <summary> Always show the backing value </summary>
        Always
    }

    public enum ConnectionType
    {
        /// <summary> Allow multiple connections</summary>
        Multiple,
        /// <summary> always override the current connection </summary>
        Override,
    }

    /// <summary> Tells which types of input to allow </summary>
    public enum TypeConstraint
    {
        /// <summary> Allow all types of input</summary>
        None,
        /// <summary> Allow connections where input value type is assignable from output value type (eg. ScriptableObject --> Object)</summary>
        AssignableTo,
        /// <summary> Allow only similar types </summary>
        Strict,
    }

    /// <summary> Iterate over all ports on this node. </summary>
    public IEnumerable<NodePort> Ports { get { foreach (NodePort port in _ports.Values) yield return port; } }
    /// <summary> Iterate over all outputs on this node. </summary>
    public IEnumerable<NodePort> Outputs { get { foreach (NodePort port in Ports) { if (port.IsOutput) yield return port; } } }
    /// <summary> Iterate over all inputs on this node. </summary>
    public IEnumerable<NodePort> Inputs { get { foreach (NodePort port in Ports) { if (port.IsInput) yield return port; } } }
    /// <summary> Iterate over all dynamic ports on this node. </summary>
    public IEnumerable<NodePort> DynamicPorts { get { foreach (NodePort port in Ports) { if (port.IsDynamic) yield return port; } } }
    /// <summary> Iterate over all dynamic outputs on this node. </summary>
    public IEnumerable<NodePort> DynamicOutputs { get { foreach (NodePort port in Ports) { if (port.IsDynamic && port.IsOutput) yield return port; } } }
    /// <summary> Iterate over all dynamic inputs on this node. </summary>
    public IEnumerable<NodePort> DynamicInputs { get { foreach (NodePort port in Ports) { if (port.IsDynamic && port.IsInput) yield return port; } } }
    /// <summary> Parent <see cref="NodeGraph"/> </summary>
    [SerializeField, HideInInspector] public NodeGraph graph;
    /// <summary> Position on the <see cref="NodeGraph"/> </summary>
    [SerializeField, HideInInspector] public Vector2 position;
    /// <summary> It is recommended not to modify these at hand. Instead, see <see cref="InputAttribute"/> and <see cref="OutputAttribute"/> </summary>
    [SerializeField, HideInInspector] private NodePortDictionary _ports = new();
    [HideInInspector] public int InstanceID;

    public string Error { get; set; } = "";

    public virtual bool ShowTitle { get; } = true;
    public abstract string Title { get; }
    public abstract float Width { get; }

    public void OnEnable()
    {
        _ports ??= new NodePortDictionary();
        InstanceID = graph.NextID;
        NodeDataCache.UpdatePorts(this, _ports);
        Init();
    }

    /// <summary> Initialize node. Called on enable. </summary>
    protected virtual void Init() { }
    public virtual void OnValidate() { }

    /// <summary> Checks all connections for invalid references, and removes them. </summary>
    public void VerifyConnections()
    {
        _ports ??= new NodePortDictionary();
        foreach (NodePort port in Ports) port.VerifyConnections();
    }

    #region Dynamic Ports
    /// <summary> Convenience function. </summary>
    /// <seealso cref="AddDynamicPort"/>
    /// <seealso cref="AddDynamicOutput"/>
    public NodePort AddDynamicInput(Type type, ConnectionType connectionType = ConnectionType.Override, TypeConstraint typeConstraint = TypeConstraint.None, string fieldName = null, bool onHeader = false)
    {
        return AddDynamicPort(type, NodePort.IO.Input, connectionType, typeConstraint, fieldName, onHeader);
    }

    /// <summary> Convenience function. </summary>
    /// <seealso cref="AddDynamicPort"/>
    /// <seealso cref="AddDynamicInput"/>
    public NodePort AddDynamicOutput(Type type, ConnectionType connectionType = ConnectionType.Override, TypeConstraint typeConstraint = TypeConstraint.None, string fieldName = null, bool onHeader = false)
    {
        return AddDynamicPort(type, NodePort.IO.Output, connectionType, typeConstraint, fieldName, onHeader);
    }

    /// <summary> Add a dynamic, serialized port to this node. </summary>
    /// <seealso cref="AddDynamicInput"/>
    /// <seealso cref="AddDynamicOutput"/>
    private NodePort AddDynamicPort(Type type, NodePort.IO direction, ConnectionType connectionType = ConnectionType.Override, TypeConstraint typeConstraint = TypeConstraint.None, string fieldName = null, bool onHeader = false)
    {
        if (fieldName == null)
        {
            fieldName = "dynamicInput_0";
            int i = 0;
            while (HasPort(fieldName)) fieldName = "dynamicInput_" + (++i);
        }
        else if (HasPort(fieldName))
        {
            Debug.LogWarning("Port '" + fieldName + "' already exists in " + GetType().Name);
            return _ports[fieldName];
        }
        NodePort port = new(fieldName, type, direction, connectionType, typeConstraint, this, onHeader);
        _ports.Add(fieldName, port);
        return port;
    }

    /// <summary> Remove an dynamic port from the node </summary>
    public void RemoveDynamicPort(string fieldName)
    {
        NodePort dynamicPort = GetPort(fieldName) ?? throw new ArgumentException("port " + fieldName + " doesn't exist");
        RemoveDynamicPort(dynamicPort);
    }

    /// <summary> Remove an dynamic port from the node </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void RemoveDynamicPort(NodePort port)
    {
        if (port.IsStatic) throw new ArgumentException("cannot remove static port");
        port.ClearConnections();
        _ports.Remove(port.fieldName);
    }

    /// <summary> Removes all dynamic ports from the node </summary>
    //[ContextMenu("Clear Dynamic Ports")]
    public void ClearDynamicPorts()
    {
        List<NodePort> dynamicPorts = [..DynamicPorts];
        foreach (NodePort port in dynamicPorts)
        {
            RemoveDynamicPort(port);
        }
    }
    #endregion

    #region Ports
    /// <summary> Returns output port which matches fieldName </summary>
    public NodePort GetOutputPort(string fieldName)
    {
        NodePort port = GetPort(fieldName);
        if (port == null || port.direction != NodePort.IO.Output) return null;
        else return port;
    }

    /// <summary> Returns input port which matches fieldName </summary>
    public NodePort GetInputPort(string fieldName)
    {
        NodePort port = GetPort(fieldName);
        if (port == null || port.direction != NodePort.IO.Input) return null;
        else return port;
    }

    /// <summary> Returns port which matches fieldName </summary>
    public NodePort GetPort(string fieldName)
    {
        if (_ports.TryGetValue(fieldName, out NodePort port)) return port;
        else return null;
    }

    public NodePort GetPort(int instanceID)
    {
        return _ports.Values.Where(p => p.InstanceID == instanceID).FirstOrDefault();
    }

    public bool HasPort(string fieldName)
    {
        return _ports.ContainsKey(fieldName);
    }
    #endregion

    #region Inputs/Outputs
    /// <summary> Return input value for a specified port. Returns fallback value if no ports are connected </summary>
    /// <param name="fieldName">Field name of requested input port</param>
    /// <param name="fallback">If no ports are connected, this value will be returned</param>
    public T GetInputValue<T>(string fieldName, T fallback = default)
    {
        NodePort port = GetPort(fieldName);
        if (port != null && port.IsConnected) return port.GetInputValue<T>();
        else return fallback;
    }

    /// <summary> Return all input values for a specified port. Returns fallback value if no ports are connected </summary>
    /// <param name="fieldName">Field name of requested input port</param>
    /// <param name="fallback">If no ports are connected, this value will be returned</param>
    public T[] GetInputValues<T>(string fieldName, params T[] fallback)
    {
        NodePort port = GetPort(fieldName);
        if (port != null && port.IsConnected) return port.GetInputValues<T>();
        else return fallback;
    }

    /// <summary> Returns a value based on requested port output. Should be overridden in all derived nodes with outputs. </summary>
    /// <param name="port">The requested port.</param>
    public virtual object GetValue(NodePort port)
    {
        Debug.LogWarning("No GetValue(NodePort port) override defined for " + GetType());
        return null;
    }
    #endregion

    /// <summary> Called after a connection between two <see cref="NodePort"/>s is created </summary>
    /// <param name="from">Output</param> <param name="to">Input</param>
    public virtual void OnCreateConnection(NodePort from, NodePort to) { }

    /// <summary> Called after a connection is removed from this port </summary>
    /// <param name="port">Output or Input</param>
    public virtual void OnRemoveConnection(NodePort port) { }

    /// <summary> Disconnect everything from this node </summary>
    public void ClearConnections()
    {
        foreach (NodePort port in Ports) port.ClearConnections();
    }

    #region Attributes
    /// <summary> Mark a serializable field as an input port. You can access this through <see cref="GetInputPort(string)"/> </summary>
    /// <param name="backingValue">Should we display the backing value for this port as an editor field? </param>
    /// <param name="connectionType">Should we allow multiple connections? </param>
    /// <param name="typeConstraint">Constrains which input connections can be made to this port </param>
    /// <param name="onHeader">Display this port on the node's header </param>
    /// <param name="dynamicPortList">If true, will display a reorderable list of inputs instead of a single port. Will automatically add and display values for lists and arrays </param>
    [AttributeUsage(AttributeTargets.Field)]
    public class InputAttribute(ShowBackingValue backingValue = ShowBackingValue.Unconnected, ConnectionType connectionType = ConnectionType.Override, TypeConstraint typeConstraint = TypeConstraint.AssignableTo, bool onHeader = false, bool dynamicPortList = false) : Attribute
    {
        public ShowBackingValue backingValue = backingValue;
        public ConnectionType connectionType = connectionType;
        public bool dynamicPortList = dynamicPortList;
        public TypeConstraint typeConstraint = typeConstraint;
        public bool onHeader = onHeader;
    }

    /// <summary> Mark a serializable field as an output port. You can access this through <see cref="GetOutputPort(string)"/> </summary>
    /// <param name="connectionType">Should we allow multiple connections? </param>
    /// <param name="typeConstraint">Constrains which input connections can be made from this port </param>
    /// <param name="onHeader">Display this port on the node's header </param>
    /// <param name="dynamicPortList">If true, will display a reorderable list of outputs instead of a single port. Will automatically add and display values for lists and arrays </param>
    [AttributeUsage(AttributeTargets.Field)]
    public class OutputAttribute(ConnectionType connectionType = ConnectionType.Multiple, TypeConstraint typeConstraint = TypeConstraint.None, bool onHeader = false, bool dynamicPortList = false) : Attribute
    {
        public ConnectionType connectionType = connectionType;
        public bool dynamicPortList = dynamicPortList;
        public TypeConstraint typeConstraint = typeConstraint;
        public bool onHeader = onHeader;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeAttribute(string menuName) : Attribute
    {
        public string category = menuName;

        public static readonly Dictionary<string, List<Type>> nodeCategories = [];

        [OnAssemblyLoad]
        [RequiresUnreferencedCode("This method is called on assembly load and will not be stripped.")]
        public static void OnAssemblyLoad()
        {
            foreach (Assembly? assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (Type type in assembly.GetTypes())
                    if (type != null)
                    {
                        NodeAttribute? attribute = type.GetCustomAttribute<NodeAttribute>();
                        if (attribute != null)
                        {
                            if (!nodeCategories.ContainsKey(attribute.category))
                                nodeCategories.Add(attribute.category, new());
                            nodeCategories[attribute.category].Add(type);
                        }
                    }
        }

        [OnAssemblyUnload]
        public static void OnAssemblyUnload()
        {
            nodeCategories.Clear();
        }

    }

    /// <summary> Prevents Node of the same type to be added more than once (configurable) to a NodeGraph </summary>
    /// <param name="max"> How many nodes to allow. Defaults to 1. </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DisallowMultipleNodesAttribute(int max = 1) : Attribute
    {
        public int max = max;
    }
    #endregion

    [Serializable]
    public class NodePortDictionary : ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> _keys = [];
        [SerializeField] private List<NodePort> _values = [];
        private Dictionary<string, NodePort> _dictionary = [];

        /// <summary>
        /// Gets the underlying dictionary of the NodePortDictionary. This dictionary should be used as read only. Changes to this dictionary will not be serialized.
        /// </summary>
        public Dictionary<string, NodePort> Dictionary => _dictionary;
        /// <summary>
        /// Gets the keys of the NodePortDictionary. Keys are ordered by insertion time..
        /// </summary>
        public List<string> Keys => _keys;
        /// <summary>
        /// Gets the values in the NodePortDictionary. Values are ordered by insertion time.
        /// </summary>
        public List<NodePort> Values => _values;

        /// <summary>
        /// Adds the specified key and value to the NodePortDictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(string key, NodePort value)
        {
            _keys.Add(key);
            _values.Add(value);
            _dictionary.Add(key, value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key.</returns>
        public NodePort this[string key]
        {
            get => _dictionary[key];
        }

        /// <summary>
        /// Determines whether the NodePortDictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the NodePortDictionary.</param>
        /// <returns>true if the NodePortDictionary contains an element with the specified key; otherwise, false.</returns>
        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns>true if the NodePortDictionary contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetValue(string key, out NodePort value)
        {
            bool result = _dictionary.TryGetValue(key, out NodePort dictionaryValue);
            value = dictionaryValue;
            return result;
        }

        /// <summary>
        /// Removes all keys and values from the NodePortDictionary
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
            _keys.Clear();
            _values.Clear();
        }

        /// <summary>
        /// Removes the value with the specified key from the NodePortDictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
        public bool Remove(string key)
        {
            if (_dictionary.ContainsKey(key))
            {
                int index = _keys.IndexOf(key);
                _keys.RemoveAt(index);
                _values.RemoveAt(index);
            }
            return _dictionary.Remove(key);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            _dictionary.Clear();

            if (_keys.Count != _values.Count)
                throw new Exception("there are " + _keys.Count + " keys and " + _values.Count + " values after deserialization. Make sure that both key and value types are serializable.");

            for (int i = 0; i < _keys.Count; i++)
                _dictionary.Add(_keys[i], _values[i]);
        }
    }
}
