﻿using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace DevBot9.Protocols.Homie {
    /// <summary>
    /// This class is useful when parsing MQTT topics trying to figure out if those topics are actually correct. If they are, then this class is later used to create a <see cref="ClientDevice"/>.
    /// </summary>
    public class ClientDeviceMetadata {
        private string _baseTopic;

        public string Id { get; internal set; } = "";
        public string HomieAttribute { get; internal set; } = "";
        public string NameAttribute { get; internal set; } = "";
        public string StateAttribute { get; internal set; } = "";
        public ClientNodeMetadata[] Nodes { get; internal set; }
        public Hashtable AllAttributes { get; internal set; } = new Hashtable();

        public ClientDeviceMetadata(string id, string baseTopic = "homie") {
            Id = id;
            _baseTopic = baseTopic;
        }

        /// <summary>
        /// Tries parsing a whole tree of a single device.
        /// </summary>
        public static bool TryParse(ArrayList topicList, string baseTopic, string deviceId, out ClientDeviceMetadata parsedClientDeviceMetadata, ref ArrayList problemList) {
            var isParsedWell = false;
            var isValidated = false;
            var candidateDevice = new ClientDeviceMetadata(deviceId, baseTopic);
            var parsedTopicList = new ArrayList();

            // It will be reassigned on successful parsing.
            parsedClientDeviceMetadata = null;

            var attributesGood = TryParseAttributes(ref topicList, ref parsedTopicList, baseTopic, ref candidateDevice, ref problemList);
            if (attributesGood) {
                var nodesGood = TryParseNodes(ref topicList, ref parsedTopicList, baseTopic, ref candidateDevice, ref problemList);
                if (nodesGood) {
                    var propertiesGood = TryParseProperties(ref topicList, ref parsedTopicList, baseTopic, ref candidateDevice, ref problemList);
                    if (propertiesGood) {
                        TrimRottenBranches(ref candidateDevice, ref problemList);
                        isParsedWell = true;
                        parsedClientDeviceMetadata = candidateDevice;
                    }
                    else {
                        problemList.Add($"Device '{candidateDevice.Id}' was detected, but it does not have a single valid property. This device subtree will be skipped.");
                    }
                }
                else {
                    problemList.Add($"Device '{candidateDevice.Id}' was detected, but it does not have a single valid node. This device subtree will be skipped.");
                }
            }
            else {
                problemList.Add($"Device '{candidateDevice.Id}' was detected, but it is missing important attributes. This device subtree will be skipped.");
            }

            return isParsedWell;
        }

        private static bool TryParseAttributes(ref ArrayList unparsedTopicList, ref ArrayList parsedTopicList, string baseTopic, ref ClientDeviceMetadata candidateDevice, ref ArrayList problemList) {
            var isParseSuccessful = false;

            // Filtering out device attributes. We'll get nodes from them.
            var deviceAttributesRegex = new Regex($@"^({baseTopic})\/({candidateDevice.Id})\/(\$[a-z0-9][a-z0-9-]+):(.+)$");
            var isHomieReceived = false;
            var isDeviceNameReceived = false;
            var isNodesReceived = false;
            var isStateReceived = false;

            foreach (string inputString in unparsedTopicList) {
                var regexMatch = deviceAttributesRegex.Match(inputString);
                if (regexMatch.Success) {
                    var key = regexMatch.Groups[3].Value;
                    var value = regexMatch.Groups[4].Value;

                    if (key == "$homie") {
                        isHomieReceived = true;
                        candidateDevice.HomieAttribute = value;
                    }
                    if (key == "$name") {
                        isDeviceNameReceived = true;
                        candidateDevice.NameAttribute = value;
                    }
                    if (key == "$state") {
                        isStateReceived = true;
                        candidateDevice.StateAttribute = value;
                    }
                    if (key == "$nodes") {
                        isNodesReceived = true;
                    }

                    candidateDevice.AllAttributes.Add(key, value);
                    parsedTopicList.Add(inputString);
                }
            }

            foreach (var parsedTopic in parsedTopicList) {
                unparsedTopicList.Remove(parsedTopic);
            }

            var minimumDeviceSetReceived = isHomieReceived & isDeviceNameReceived & isNodesReceived & isStateReceived;

            if (minimumDeviceSetReceived) {
                isParseSuccessful = true;
            }
            else {
                isParseSuccessful = false;
                if (isHomieReceived == false) { problemList.Add($"Device '{candidateDevice.Id}': mandatory attribute $homie was not found, parsing cannot continue."); }
                if (isDeviceNameReceived == false) { problemList.Add($"Device '{candidateDevice.Id}': mandatory attribute $name was not found, parsing cannot continue."); }
                if (isNodesReceived == false) { problemList.Add($"Device '{candidateDevice.Id}': mandatory attribute $nodes was not found, parsing cannot continue."); }
                if (isStateReceived == false) { problemList.Add($"Device '{candidateDevice.Id}': mandatory attribute $state was not found, parsing cannot continue."); }
            }

            return isParseSuccessful;
        }
        private static bool TryParseNodes(ref ArrayList unparsedTopicList, ref ArrayList parsedTopicList, string baseTopic, ref ClientDeviceMetadata candidateDevice, ref ArrayList problemList) {
            var isParseSuccessful = true;

            var candidateNodeIds = ((string)candidateDevice.AllAttributes["$nodes"]).Split(',');
            var goodNodes = new ArrayList();

            for (var n = 0; n < candidateNodeIds.Length; n++) {
                var candidateNode = new ClientNodeMetadata() { Id = candidateNodeIds[n] };

                // Filtering out attributes for this node. We'll get properties from them.
                var nodeAttributesRegex = new Regex($@"^({baseTopic})\/({candidateDevice.Id})\/({candidateNode.Id})\/(\$[a-z0-9][a-z0-9-]+):(.+)$");
                foreach (string inputString in unparsedTopicList) {
                    var regexMatch = nodeAttributesRegex.Match(inputString);
                    if (regexMatch.Success) {
                        var key = regexMatch.Groups[4].Value;
                        var value = regexMatch.Groups[5].Value;

                        if (key == "$name") {
                            candidateNode.NameAttribute = value;
                        }
                        if (key == "$type") {
                            candidateNode.TypeAttribute = value;
                        }

                        candidateNode.AllAttributes.Add(regexMatch.Groups[4].Value, regexMatch.Groups[5].Value);
                        parsedTopicList.Add(inputString);
                    }
                }

                foreach (var parsedTopic in parsedTopicList) {
                    unparsedTopicList.Remove(parsedTopic);
                }

                // Figuring out properties we have for this node.
                if (candidateNode.AllAttributes.Contains("$properties")) {
                    goodNodes.Add(candidateNode);
                }
                else {
                    // Something is wrong, an essential topic is missing.
                    problemList.Add($"Node '{candidateNode.Id}' of device '{candidateDevice.Id}' is defined, but $properties attribute is missing. This node subtree will be skipped entirely.");
                }
            }

            // Should be at least one valid node. If not - problem.
            if (goodNodes.Count == 0) isParseSuccessful = false;

            // Converting local temporary lists to final arrays and returning.
            candidateDevice.Nodes = new ClientNodeMetadata[goodNodes.Count];
            for (var i = 0; i < goodNodes.Count; i++) {
                candidateDevice.Nodes[i] = (ClientNodeMetadata)goodNodes[i];
            }

            return isParseSuccessful;
        }
        private static bool TryParseProperties(ref ArrayList unparsedTopicList, ref ArrayList parsedTopicList, string baseTopic, ref ClientDeviceMetadata candidateDevice, ref ArrayList problemList) {
            var isParseSuccessful = false;

            for (var n = 0; n < candidateDevice.Nodes.Length; n++) {
                var candidatePropertyIds = ((string)candidateDevice.Nodes[n].AllAttributes["$properties"]).Split(',');
                var goodProperties = new ArrayList();

                for (var p = 0; p < candidatePropertyIds.Length; p++) {
                    var candidateProperty = new ClientPropertyMetadata() { NodeId = candidateDevice.Nodes[n].Id, PropertyId = candidatePropertyIds[p] };

                    // Parsing property attributes and value.
                    var attributeRegex = new Regex($@"^({baseTopic})\/({candidateDevice.Id})\/({candidateDevice.Nodes[n].Id})\/({candidateProperty.PropertyId})(\/\$[a-z0-9][a-z0-9-]+)?(:)(.+)$");
                    var setRegex = new Regex($@"^({baseTopic})\/({candidateDevice.Id})\/({candidateDevice.Nodes[n].Id})\/({candidateProperty.PropertyId})(\/set)(:)(.+)$");
                    var valueRegex = new Regex($@"^({baseTopic})\/({candidateDevice.Id})\/({candidateDevice.Nodes[n].Id})\/({candidateProperty.PropertyId})()(:)(.+)$");

                    var isSettable = false;
                    var isRetained = false;
                    var isNameReceived = false;
                    var isDataTypeReceived = false;
                    var isSettableReceived = false;
                    var isRetainedReceived = false;

                    foreach (string inputString in unparsedTopicList) {
                        var attributeMatch = attributeRegex.Match(inputString);
                        if (attributeMatch.Success) {
                            var key = attributeMatch.Groups[5].Value;
                            var value = attributeMatch.Groups[7].Value;

                            if (key == "/$name") {
                                candidateProperty.Name = value;
                                isNameReceived = true;
                            }
                            if (key == "/$datatype") {
                                if (Helpers.TryParseHomieDataType(value, out var parsedType)) {
                                    candidateProperty.DataType = parsedType;
                                    isDataTypeReceived = true;
                                };
                            }
                            if (key == "/$format") {
                                candidateProperty.Format = value;
                            }
                            if (key == "/$settable") {
                                if (Helpers.TryParseBool(value, out isSettable)) {
                                    isSettableReceived = true;
                                }
                            }
                            if (key == "/$retained") {
                                if (Helpers.TryParseBool(value, out isRetained)) {
                                    isRetainedReceived = true;
                                };
                            }

                            if (key == "/$unit") {
                                candidateProperty.Unit = value;
                            }

                            parsedTopicList.Add(inputString);
                        }

                        var setMatch = setRegex.Match(inputString);
                        if (setMatch.Success) {
                            // Discarding this one. This is a historically cached command, and these should not execute during initialization. Besides, it shouldn't be retained,
                            // so the fact that we're here means something is wrong with the host side.
                            problemList.Add($"Device '{candidateDevice.Id}', property {candidateProperty} has /set topic assigned, which means /set message is published as retained. This is against Homie convention.");
                        }

                        var valueMatch = valueRegex.Match(inputString);
                        if (valueMatch.Success) {
                            var value = attributeMatch.Groups[7].Value;
                            candidateProperty.InitialValue = value;
                        }
                    }

                    foreach (var parsedTopic in parsedTopicList) {
                        unparsedTopicList.Remove(parsedTopic);
                    }

                    // Basic data extraction is done. Now we'll validate if values of the fields are compatible with each other and also YAHI itself.
                    var isOk = isNameReceived & isDataTypeReceived & isSettableReceived & isRetainedReceived;
                    if (isOk) {
                        // Setting property type.
                        if ((isSettable == false) && (isRetained == true)) {
                            candidateProperty.PropertyType = PropertyType.State;
                        }
                        if ((isSettable == true) && (isRetained == false)) {
                            candidateProperty.PropertyType = PropertyType.Command;
                        }
                        if ((isSettable == true) && (isRetained == true)) {
                            candidateProperty.PropertyType = PropertyType.Parameter;
                        }
                        if ((isSettable == false) && (isRetained == false)) {
                            problemList.Add($"Property '{candidateProperty.PropertyId}' of node '{candidateProperty.NodeId}' of device '{candidateDevice.Id}' is has all mandatory fields set, but this retainability and settability configuration is not supported by YAHI. Skipping this property entirely.");
                            isOk = false;
                        }

                    }
                    else {
                        // Some of the mandatory topic were not received. Can't let this property through.
                        problemList.Add($"Property '{candidateProperty.PropertyId}' of node '{candidateProperty.NodeId}' of device '{candidateDevice.Id}' is defined, but mandatory attributes are missing. Entire property subtree will be skipped.");
                        isOk = false;
                    }

                    // Validating by property data type, because rules are very different for each of those.
                    if (isOk) {
                        var isNotCommand = candidateProperty.PropertyType != PropertyType.Command;
                        switch (candidateProperty.DataType) {
                            case DataType.String:
                                // String type has pretty much no restriction to attributes content.
                                break;

                            case DataType.Integer:
                                if (isNotCommand && (Helpers.IsInteger(candidateProperty.InitialValue) == false)) {
                                    problemList.Add($"Error:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId} is set to {candidateProperty.InitialValue}, which is not a valid initial value for integer data type. Skipping this property entirely.");
                                    isOk = false;
                                }
                                break;

                            case DataType.Float:
                                if (isNotCommand && (Helpers.IsFloat(candidateProperty.InitialValue) == false)) {
                                    problemList.Add($"Error:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId} is set to {candidateProperty.InitialValue}, which is not a valid initial value for float data type. Skipping this property entirely.");
                                    isOk = false;
                                }
                                break;

                            case DataType.Boolean:
                                if (isNotCommand && (Helpers.TryParseBool(candidateProperty.InitialValue, out _) == false)) {
                                    problemList.Add($"Error:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId} is set to {candidateProperty.InitialValue}, which is not a valid initial value for boolean data type. Skipping this property entirely.");
                                    isOk = false;
                                }

                                if (isOk) {
                                    if (candidateProperty.Format != "") {
                                        problemList.Add($"Warning:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId}.$format attribute is {candidateProperty.Format}. Should be empty for boolean data type. Clearing it.");
                                        candidateProperty.Format = "";
                                    }
                                    if (candidateProperty.Unit != "") {
                                        problemList.Add($"Warning:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId}.$unit attribute is {candidateProperty.Unit}. Should be empty for boolean data type. Clearing it.");
                                        candidateProperty.Unit = "";
                                    }
                                }
                                break;

                            case DataType.Enum:
                                if (candidateProperty.Format == "") {
                                    problemList.Add($"Warning:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId}.$format attribute is empty, which is not valid for enum data type. Skipping this property entirely.");
                                    isOk = false;
                                }
                                var options = candidateProperty.Format.Split(',');

                                if (isOk) {
                                    var isInitialValueCorrect = false;
                                    foreach (var option in options) {
                                        if (candidateProperty.InitialValue == option) {
                                            isInitialValueCorrect = true;
                                        }
                                    }

                                    if (isNotCommand && (isInitialValueCorrect == false)) {
                                        problemList.Add($"Error:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId} is set to {candidateProperty.InitialValue}, while it should be one of {candidateProperty.Format}. Skipping this property entirely.");
                                        isOk = false;
                                    }
                                }
                                break;

                            case DataType.Color:
                                if (candidateProperty.Format == "") {
                                    problemList.Add($"Warning:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId}.$format attribute is empty, which is not valid for color data type. Skipping this property entirely.");
                                    isOk = false;
                                }

                                if (isOk) {
                                    if (Helpers.TryParseHomieColorFormat(candidateProperty.Format, out var colorFormat) == false) {
                                        problemList.Add($"Warning:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId}.$format attribute is {candidateProperty.Format}, which is not valid for color data type. Skipping this property entirely.");
                                        isOk = false;
                                    }
                                    else if (isNotCommand) {
                                        if (HomieColor.ValidatePayload(candidateProperty.InitialValue, colorFormat) == false) {
                                            problemList.Add($"Error:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId} is set to {candidateProperty.InitialValue}, which is not valid for color format {colorFormat}. Skipping this property entirely.");
                                            isOk = false;
                                        }
                                    }
                                }
                                break;

                            case DataType.DateTime:
                            case DataType.Duration:
                                problemList.Add($"Error:{candidateDevice.Id}.{candidateProperty.NodeId}.{candidateProperty.PropertyId} is of type {candidateProperty.DataType}, but this is not currently supported by YAHI. Skipping this property entirely.");
                                isOk = false;
                                break;
                        }
                    }

                    if (isOk) {
                        goodProperties.Add(candidateProperty);
                    }

                }

                // Converting local temporary property lists to final arrays.
                candidateDevice.Nodes[n].Properties = new ClientPropertyMetadata[goodProperties.Count];
                for (var i = 0; i < goodProperties.Count; i++) {
                    candidateDevice.Nodes[n].Properties[i] = (ClientPropertyMetadata)goodProperties[i];
                }
            }

            // Converting local temporary node lists to final arrays and returning.
            foreach (var node in candidateDevice.Nodes) {
                if (node.Properties.Length > 0) {
                    isParseSuccessful = true;
                }
            }

            return isParseSuccessful;
        }
        private static void TrimRottenBranches(ref ClientDeviceMetadata candidateDevice, ref ArrayList problemList) {
            var goodNodes = new ArrayList();
            var newNodesValue = "";
            var updateNeeded = false;

            // Nodes have $properties attribute, where all the properties are listed. This list must be synchronized with actual properties.
            // The count may differ because some properties may have been rejected if there's some crucial data missing.
            foreach (var node in candidateDevice.Nodes) {
                if (node.Properties.Length > 0) {
                    goodNodes.Add(node);

                    var shouldBeThatManyProperties = ((string)node.AllAttributes["$properties"]).Split(',').Length;
                    if (node.Properties.Length != shouldBeThatManyProperties) {
                        var newAttributeValue = node.Properties[0].PropertyId;
                        for (var i = 1; i < node.Properties.Length; i++) {
                            newAttributeValue += "," + node.Properties[i].PropertyId;
                        }
                        node.AllAttributes["$properties"] = newAttributeValue;
                        updateNeeded = true;
                    }
                }
                else {
                    updateNeeded = true;
                }
            }

            // Same with device, it has $nodes synced with actual node count.
            if (goodNodes.Count > 0) {
                var shouldBeThatManyNodes = ((string)candidateDevice.AllAttributes["$nodes"]).Split(',').Length;
                if (goodNodes.Count != shouldBeThatManyNodes) {
                    newNodesValue = ((ClientNodeMetadata)goodNodes[0]).Id;
                    for (var i = 1; i < goodNodes.Count; i++) {
                        newNodesValue += "," + ((ClientNodeMetadata)goodNodes[i]).Id;
                    }
                    updateNeeded = true;
                }
            }
            else {
                throw new InvalidOperationException("There should be at least a single good property at this point. Something is internally wrong with parser.");
            }

            // If needed, trimming the actual structures.
            if (updateNeeded) {
                candidateDevice.AllAttributes["$nodes"] = newNodesValue;
                candidateDevice.Nodes = new ClientNodeMetadata[goodNodes.Count];
                for (var i = 0; i < goodNodes.Count; i++) {
                    candidateDevice.Nodes[i] = (ClientNodeMetadata)goodNodes[i];
                }

                problemList.Add($"Device {candidateDevice.Id} has been trimmed. Values of the fields will not be identical to what's on the broker topics!");
            }
        }

        public override string ToString() {
            return Id;
        }
    }
}
