using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

// This code should be capable of something like what you see here, if you want an introduction
// https://www.youtube.com/watch?v=l1prct6Xxzw  Start the video at 56s


namespace MeshSim
{

    // Interface, etc.
    class RFNET
    {
        // Can be changed manually to turn debug info on/off
        public static bool debugging = false;
        public static long secondsToMap = 0;
        public static List<int> offlineNodeList = new List<int>();

        static void Main()
        {
            Console.WriteLine("\nPlease enter a command.  Enter h for help");
            // Get the command from the user
            List<string> fullCommand = GetCommand();

            switch (fullCommand[0])
            {
                case "g":
                    networkOps.GenerateNetwork();
                    Main();
                    break;
                case "ga":
                    networkOps.GenerateNetwork(true);
                    Main();
                    break;
                case "gm":
                    Console.WriteLine("Generating advanced (modified) network.  Last node will not be online.");
                    networkOps.GenerateNetwork(false, true);
                    Main();
                    break;
                case "s":
                    // Save the currently generated network
                    Console.WriteLine("Saving network topology...");
                    networkOps.SaveNetwork();
                    Console.WriteLine("Done!");
                    Main();
                    break;
                case "l":
                    Console.WriteLine("Loading saved network");
                    networkOps.LoadNetwork();
                    Console.WriteLine("Done!");
                    networkOps.PrintNetwork();
                    Main();
                    break;
                case "d":
                    Console.WriteLine("Deleting existing topology");
                    MeshSim.networkOps.meshNet.Clear();
                    offlineNodeList.Clear();
                    Console.WriteLine("Done! Enter g to generate a new network");
                    Main();
                    break;
                case "c":
                    Console.WriteLine("Checking validity of maps");
                    networkOps.CheckValidity();
                    Main();
                    break;
                case "p":
                    Console.WriteLine("Packets transmitted: " + RFNET.packetCount);
                    Main();
                    break;
                case "pa":
                    Console.WriteLine("ACK count: " + RFNET.ackCount);
                    Main();
                    break;
                case "h":
                    Console.WriteLine(@"
You've entered help.
Enter 'g' to generate a new network. (no mappings, all online. deprecated)
Enter 'ga' generate advanced: to generate a network where all nodes are powered off to start with.
Enter 'gm' to generate a modified network: all nodes on (not mapped) except last one.
Enter 's' to save the generated network (without mappings)
Enter 'l' to load the previously-saved network
Enter 'v' to open the most recent graphml output (You need to install a graphml grapher like yEd)
Enter 'c' to check validity of network maps
Enter 'd' to delete the current network (doesn't delete file)
Enter 'off <nodeID> <nodeID>' to turn nodes off
Enter 'on <nodeID> <nodeID> ...<nodeID>' to turn nodes on
Enter 'ls' to show all ONLINE nodes.
Enter 'll' to show ALL nodes.
Enter 'send <fromID> <toID>' to manually send a packet.
Enter 'sho <nodeID>' to see the routing table of a node.
Enter 'alloff' to turn all nodes off.
Enter 'randomon' to turn all nodes on in a random order.
Enter 'sequentialon' to turn all nodes on in sequential order.
Enter 'alloff' to turn all nodes off immediately.
Enter 'hb <nodeID> <nodeID> ...<nodeID>' to trigger heartbeat on a node
Enter 'hball' to trigger a heartbeat on each node in random order
");
                    Main();
                    break;
                case "off":
                    if (fullCommand.Count < 2)
                        Console.WriteLine("Usage: 'off <nodeID> <nodeID> ... <nodeID>'");

                    if (fullCommand.Count > 1)
                    {
                        // Iterate through the nodes that were listed
                        for (int i = 1; i < fullCommand.Count; i++)
                        {
                            offlineNodeList.Add(Convert.ToInt32(fullCommand[i]));
                            Console.WriteLine("Powered off node " + fullCommand[i]);
                        }
                    }
                    Main();
                    break;
                case "on":
                    if (fullCommand.Count < 2)
                        Console.WriteLine("Usage: 'on <nodeID> <nodeID> ... <nodeID>'");

                    if (fullCommand.Count > 1)
                    {
                        // Iterate through the nodes that were listed
                        for (int i = 1; i < fullCommand.Count; i++)
                        {
                            networkOps.meshNet[Convert.ToInt32(fullCommand[i]) - 1].start();
                            Console.WriteLine("Powered up node " + fullCommand[i]);
                        }
                    }
                    Main();
                    break;
                case "ls":
                    Console.WriteLine("Printing network");
                    // Pass in false for optional bool showBothOnAndOff parameter
                    networkOps.PrintNetwork(false);
                    Main();
                    break;
                case "ll":
                    Console.WriteLine("Printing network");
                    networkOps.PrintNetwork();
                    Main();
                    break;
                case "send":
                    if (fullCommand.Count < 2)
                        Console.WriteLine("Usage: 'ping <fromID> <toID>'");

                    int inputFrom = Convert.ToInt32(fullCommand[1]);
                    int inputTo = Convert.ToInt32(fullCommand[2]);

                    // Create and send the packet
                    packet oneWayPingPacket = new packet();
                    oneWayPingPacket = packetBuilder.CreateOneWayPing(inputTo);
                    oneWayPingPacket.currentDestination = networkOps.meshNet[inputFrom - 1].pathDictionary[inputTo];
                    oneWayPingPacket.previousDestination = inputFrom;
                    networkOps.meshNet[inputFrom - 1].SendPacket(oneWayPingPacket);

                    Main();
                    break;
                //case "send": // <--- deprecated
                //    Console.WriteLine("Sending general packet");
                //    Console.WriteLine("From:");
                //    int genFrom = stringToInt(Console.ReadLine());
                //    Console.WriteLine("To:");
                //    int genTo = stringToInt(Console.ReadLine());
                //    Console.WriteLine("Message");
                //    string payload = Console.ReadLine();
                //    packet genPacket = packetBuilder.CreateGeneralDataPacket(genTo,payload);
                //    genPacket.origin = genFrom;
                //    genPacket.currentDestination = networkOps.meshNet[genFrom - 1].pathDictionary[genTo];
                //    genPacket.previousDestination = genFrom;
                //    networkOps.meshNet[genFrom - 1].SendPacket(genPacket);
                //    Main();
                //    break;
                case "sho":
                    int shoNode = Convert.ToInt32(fullCommand[1]);
                    networkOps.meshNet[shoNode - 1].ShowPaths();
                    Main();
                    break;
                case "alloff":
                    Console.WriteLine("\nTurning all nodes off");
                    networkOps.AllOff();
                    Main();
                    break;
                case "randomon":
                    Console.WriteLine("Turning on all nodes in random order");
                    networkOps.randomOn();
                    Console.WriteLine("Transmitted " + packetCount + " packets.");
                    Main();
                    break;
                case "sequentialon":
                    Console.WriteLine("Turning on all nodes in sequential order");
                    networkOps.sequentialOn();
                    Console.WriteLine("Transmitted " + packetCount + " packets.");
                    Main();
                    break;
                case "map":
                    int youMap = Convert.ToInt32(fullCommand[1]);// stringToInt(Console.ReadLine());
                    networkOps.meshNet[youMap - 1].DoMap();
                    Main();
                    break;
                case "hb":
                    if (fullCommand.Count < 2)
                        Console.WriteLine("Usage: 'hb <nodeID> <nodeID> ... <nodeID>'");

                    if (fullCommand.Count > 1)
                    {
                        // Iterate through the nodes that were listed
                        for (int i = 1; i < fullCommand.Count; i++)
                        {
                            networkOps.meshNet[Convert.ToInt32(fullCommand[i]) - 1].Heartbeat();
                        }
                    }
                    Main();
                    break;
                case "hball":
                    Console.WriteLine("Starting random but complete heartbeat triggering.");
                    networkOps.heartbeatAll();
                    Console.WriteLine("\nEach node in the network did a heartbeat in a random order.");
                    Main();
                    break;
                case "killrandom":
                    networkOps.killRandom();
                    Main();
                    break;
                case "v":
                    // Show the graphml
                    if (File.Exists("ExportedNetwork.graphml")) { Process.Start("ExportedNetwork.graphml"); }
                    else { Console.WriteLine("You have not yet saved a network.  Generate one, then enter 's' to save it."); }
                    Main();
                    break;
                case "showtree":
                    int nodeOfInterest = Convert.ToInt32(fullCommand[1]);
                    networkOps.meshNet[nodeOfInterest - 1].ShowTree();
                    Main();
                    break;
                case "pt": // process tree
                    int ptNode = Convert.ToInt32(fullCommand[1]);
                    networkOps.meshNet[ptNode - 1].ProcessTree();
                    Main();
                    break;
                default:
                    // Complain
                    Console.WriteLine("WAT!?!?!");
                    Main();
                    break;
            }


        }

        // Gets command from input string
        private static List<string> GetCommand()
        {
            // Read line from user
            string fullInput = Console.ReadLine();

            // Split input into a list
            return new List<string>(fullInput.Split(' '));

        }

        // Count of transmitted packets (per launch)
        public static int packetCount = 0;

        // Count of ACK packets (per launch)
        public static int ackCount = 0;

        // Safely checks and coverts a string of user-entered data into an integer
        private static int stringToInt(string input)
        {
            try
            {
                int output = Convert.ToInt32(input);
                return output;
            }
            catch (Exception e)
            {
                Console.WriteLine("Please enter a valid integer.");
                return stringToInt(Console.ReadLine());
            }
        }

    }

    // THE node object
    public class node
    {
        // Equivalent to MAC address
        public int nodeID;

        // List of physNeighbors as defined by physical layout simulation
        public List<int> physNeighbors = new List<int>(); // SIM ONLY

        // Status of this node (used in mapping)
        public List<int> visited = new List<int>();

        // Copy of physNeighbors as discovered by the network itself
        public List<int> neighbors = new List<int>();

        // Version of master map
        public int mapVersion;

        // Map of the whole network
        // Dictionary of each child node (keys) to the next hop from here
        [XmlIgnore()]
        public Dictionary<int, int> pathDictionary = new Dictionary<int, int>();

        // Proper BFS listing of parents/children.  This is NOT a next-hop finder like pathDictionary
        [XmlIgnore()]
        public Dictionary<int, List<int>> meshTree = new Dictionary<int, List<int>>();

        // Safely adds parent/child relationship to the tree
        public void AddToTree(int parent, int child, bool calledFromOutside = true)
        {
            if(!meshTree.ContainsKey(parent))
            {
                meshTree[parent] = new List<int>(child);
            }
            else if (!meshTree[parent].Contains(child))
            {
                meshTree[parent].Add(child);
            }

            // Sew up the back side by reversing the parameters.  Boolean ensures this doesn't recurse forever.
            if(calledFromOutside)
                AddToTree(child, parent, /*calledFromOutside*/false);

        }

        // Send packet   PORTED
        public void SendPacket(packet outgoingPacket)
        {
            if (RFNET.debugging /*|| outgoingPacket.packetType == 9*/)
            {
                Console.WriteLine("Sending packet:\tFrom " + nodeID + " to: " + outgoingPacket.currentDestination +
                " final destination: " + outgoingPacket.finalDestination + " type: " + outgoingPacket.packetType + " payload: " + outgoingPacket.payload);
            }
            RFNET.packetCount++;  // SIM ONLY
            // Note that we don't send the packet directly to the destination node object, but rather we
            //  send it to the nodes that are in range, no matter who they are.  This way we can simulate
            //  ranged communication between non-adjacent nodes.
            List<int> onlinePhysNeighbors = new List<int>(physNeighbors);  //SIM

            foreach (int offline in RFNET.offlineNodeList)
            {
                onlinePhysNeighbors.Remove(offline);  // SIM
            }

            foreach (int physNeighbor in onlinePhysNeighbors)
            {

                // Fake sendig a packet by simply telling the nodes in range to receive the packet object
                networkOps.meshNet[physNeighbor - 1].ReceivePacket(outgoingPacket);  //SIM
            }

        }

        // Receive packet  
        private void ReceivePacket(packet incomingPacket)
        {
            // Since every packet send from node x will be seen by all of x's neighbors.
            // Each receiving node needs to decide if the node is for "me" or not.
            // Broadcasts sent to destination 255 will be accepted by all nodes.
            if (incomingPacket.currentDestination == nodeID || incomingPacket.currentDestination == 255)
            {

                // Eavesdropping
                if (incomingPacket.packetType == 4 && incomingPacket.payload.Length > 0)
                {
                    foreach (string remoteNeighbor in incomingPacket.payload.Split(','))
                    {
                        int remoteNeighborID = Convert.ToInt32(remoteNeighbor);
                        //meshTree[remoteNeighborID].Add(incomingPacket.origin);
                        AddToTree(remoteNeighborID, incomingPacket.origin);

                        if (remoteNeighborID != 0 && remoteNeighborID != nodeID/* && !neighbors.Contains(remoteNeighborID)/* && !pathDictionary.ContainsKey(rn)*/) // <---- Commented bug causes nodes to pass back and forth to each other (stack overflow)
                        {
                            //networkOps.meshNet <-- for viewing network contents in VS
                            pathDictionary[remoteNeighborID] = incomingPacket.previousDestination;
                            //AddToTree(remoteNeighborID, incomingPacket.origin);
                        }
                    }

                    // Type 4 packet was actually for me, so tell the user it worked.
                    if (/*!visited.Contains(incomingPacket.origin)*/incomingPacket.finalDestination == nodeID)
                    {
                        visited.Add(incomingPacket.origin);
                        if (RFNET.debugging) { Console.WriteLine("Node " + nodeID + " has visited " + incomingPacket.origin + " by node " + nodeID); }

                    }
                }

                // If we are not the final destination, pass it on
                if (incomingPacket.currentDestination != incomingPacket.finalDestination)
                {
                    // Just before we shoot this packet off as a hop, let's scrape out some useful info!
                    // The origin of this packet is accessible via the previousDestination
                    // Add this to the local path.  Hops can map out far away places like this.


                    // We are a hop; Change the currentDestination to the next hop on the way to the final.
                    //  Execute lookup on the local pathDictionary.


                    // This following bit isn't necessary, but it can help to reinforce mappings.  I'd rather handle this with heartbeats though
                    //if (!pathDictionary.Keys.Contains(incomingPacket.origin) && incomingPacket.origin != nodeID && incomingPacket.origin != 0)
                    //{
                    //    pathDictionary[incomingPacket.origin] = incomingPacket.previousDestination;
                    //}

                    if(incomingPacket.packetType == 9)
                    {
                        List<string> strPayload = new List<string>(incomingPacket.payload.Split('_'));
                        strPayload.Remove("");

                        if(Convert.ToInt32(strPayload[1]) != mapVersion || pathDictionary[incomingPacket.finalDestination] == incomingPacket.previousDestination)
                        {
                            if (pathDictionary.ContainsKey(incomingPacket.finalDestination) &&
                                pathDictionary[incomingPacket.finalDestination] == incomingPacket.previousDestination)
                            {
                                //ShowTree();

                                /*Console.WriteLine("\n\nCONFLICT DETECTED: Node " + nodeID + " shows that the NH to " + incomingPacket.finalDestination + " is " +
                                    pathDictionary[incomingPacket.finalDestination] + " which is the same as the previous destination " + incomingPacket.previousDestination +
                                    " This will likely cause a back-and-forth problem and a stack overflow. There is potentially a \"ring\" in the network.  Will now attempt " + 
                                    "to process the tree in this packet to find another path to the destination.\n"); */
                                    ProcessIncomingMap(incomingPacket, /*pokingDisabled*/ true);
                                
                                //Console.WriteLine("\n Map version " + mapVersion + " (just installed) now shows that the NH to " + incomingPacket.finalDestination +
                                //    " is " + pathDictionary[incomingPacket.finalDestination]);

                                //if (incomingPacket.finalDestination == pathDictionary[incomingPacket.finalDestination])
                                //    Console.WriteLine("\nLooks like we were missing a neighbor.  Continuing...");
                                //else
                                    //Console.ReadLine(); // Wait for user to read message.

                            }
                        }

                        // Forcefully release memory to avoid stack overflow
                        //GC.Collect();//strPayload;
                        strPayload.Clear();
                        
                    }

                    try
                    {
                        incomingPacket.currentDestination = pathDictionary[incomingPacket.finalDestination];
                        incomingPacket.previousDestination = nodeID;

                    }
                    catch(Exception e)
                    {
                        // Go ahead and process the tree to see if we can find the next hop
                        if(incomingPacket.packetType == 9)
                        {
                            ProcessIncomingMap(incomingPacket);
                        }

                        //Console.WriteLine("\n\tNo NH record in node " + nodeID + " for FD " + incomingPacket.finalDestination + "\n");
                        //Console.WriteLine();
                    }

                    SendPacket(incomingPacket);


                }
                else
                {
                    // Determine aciton based on packet type
                    switch (incomingPacket.packetType)
                    {
                        // This is an incoming DiscoveryRequest packet
                        case 1:
                            // Add the sending node (which is definitely a neighbor) to the pathDictionary
                            if (incomingPacket.origin != 0 /*&& !pathDictionary.ContainsKey(incomingPacket.origin)*/)
                            {
                                pathDictionary[incomingPacket.origin] = incomingPacket.origin; // This row in the DF/NH table looks like [5,5] or [8,8] or [n,n]
                            }
                            if (!neighbors.Contains(incomingPacket.origin)) // Make sure we include the neighbor as a neighbor
                            {
                                neighbors.Add(incomingPacket.origin);
                            }

                            // Respond
                            packet discoverResponse = new packet();
                            discoverResponse = packetBuilder.CreateResponsePacket(incomingPacket);
                            discoverResponse.origin = nodeID;
                            discoverResponse.previousDestination = nodeID;
                            SendPacket(discoverResponse);
                            break;
                        // This is an incoming DiscoveryResponse packet
                        case 2:
                            // Do not generate a response, instead, add response ID to our physNeighbors list.
                            // Add this one as a direct neighbor
                            if (!neighbors.Contains(incomingPacket.origin))
                            {
                                neighbors.Add(incomingPacket.origin);
                            }
                            // Also add these nodes to the directory.
                            //if (!pathDictionary.ContainsKey(incomingPacket.origin)) <-- back and forth bug live here too
                            //{
                            pathDictionary[incomingPacket.origin] = incomingPacket.origin; // This row in the DF/NH table looks like [5,5] or [8,8] or [n,n]
                            //meshTree[nodeID].Add(incomingPacket.origin); // I am the preferred parent for this node
                            AddToTree(nodeID, incomingPacket.origin);
                            //}
                            break;
                        // This is an incoming GetNeighbors 
                        case 3:
                            // Remote get neighbors request
                            // Incoming 3 must refresh neighbor list before sending <-- really?  This is important, though I'm not entirely sure why it was
                            // Go ahead and add the previous node (sending hop) as the path to the origin of the packet
                            //if (!pathDictionary.ContainsKey(incomingPacket.origin)) <-- Cuases responses to go back on a different path than they came from.  That's bad.
                            //{
                            pathDictionary[incomingPacket.origin] = incomingPacket.previousDestination; // We do want to do this here because we might not know where to send the response.
                            //}

                            // Send back a list of neighbors
                            // Clear neighbors list
                            neighbors.Clear(); // <--- This stupid line was missing for a while...caused a big bug. APPRECIATE THIS LINE FOR A MOMENT!

                            
                            // Do a local mapping of my own neighbors
                            packet discoverPacket = packetBuilder.CreateDiscoverRequestPacket();
                            discoverPacket.origin = nodeID;
                            discoverPacket.previousDestination = nodeID;
                            SendPacket(discoverPacket);
                            

                            // Compile my neighbors into a string, then send that string in the payload of a type 4 response
                            string neighborListString = "";
                            foreach (int n in neighbors)
                            {
                                neighborListString += n + ",";
                            }
                            neighborListString = neighborListString.TrimEnd(',');

                            // Generate response packet
                            packet response = packetBuilder.CreateNeighborsPacket();
                            response.currentDestination = pathDictionary[incomingPacket.origin]; // <-- Lookup value stored a few lines ago
                            response.finalDestination = incomingPacket.origin;
                            response.origin = nodeID;
                            response.payload = neighborListString;
                            response.previousDestination = nodeID;
                            SendPacket(response);

                            break;
                        // This is an incoming Neighbors packet
                        case 4:
                            // Case 4 stuff is handled by each node that touches it.  Look above the switch statement.
                            break;
                        // This is an incoming pokePacket.  My turn to map!
                        case 6:
                            DoMap();

                            //PokeNext(incomingPacket.origin);
                            break;
                        // This is an incoming masterTree
                        case 9:
                            ProcessIncomingMap(incomingPacket);
                            break;
                    }

                }
            }
        }

        // Poke the next node to start mapping
        /* pokeNext is only activated from the wwMap 
         * 
         */

        private void ProcessIncomingMap(packet incomingPacket, bool pokingDisabled = false)
        {
            // Prepare to format incoming tree
            Dictionary<int, List<int>> masterTree = new Dictionary<int, List<int>>();

            string strTree = incomingPacket.payload;

            List<string> strDataVersion = new List<string>(strTree.Split('_'));
            strDataVersion.Remove("");

            int version = Convert.ToInt32(strDataVersion[1]);

            if (version != mapVersion)
            {
                List<string> parentChildSets = new List<string>(strDataVersion[0].Split('|'));
                parentChildSets.Remove("");

                foreach (string s in parentChildSets)
                {
                    int parent = Convert.ToInt32(s.Split(':')[0]);
                    List<string> strChildren = new List<string>(s.Split(':')[1].Split(','));
                    strChildren.Remove(string.Empty);
                    List<int> children = strChildren.Select(k => int.Parse(k)).ToList();  // http://stackoverflow.com/questions/6201306/how-to-convert-liststring-to-listint

                    foreach (int c in children)
                    {
                        if (!masterTree.Keys.Contains(parent))
                            masterTree[parent] = new List<int>();

                        masterTree[Convert.ToInt32(parent)].Add(c);
                    }

                }
                meshTree = masterTree;
                if (RFNET.debugging) { Console.WriteLine("Current map version " + mapVersion + " being replaced with version " + version); }
                ProcessTree(version);
            }

            if(!pokingDisabled)
                PokeNext(incomingPacket);
            
        }
        private void PokeNext(packet passAlongNewMaster)
        {
            
            List<int> allNodes = pathDictionary.Keys.ToList();
            allNodes.Sort();
            bool foundSomeoneToPoke = false;

            foreach (int i in allNodes)
            {
                if (foundSomeoneToPoke) // Not efficient.  TODO: Improve?
                    continue;

                if (i > nodeID && i != nodeID && i != passAlongNewMaster.origin)
                {
                    foundSomeoneToPoke = true;
                    passAlongNewMaster.currentDestination = pathDictionary[i];
                    passAlongNewMaster.previousDestination = nodeID;
                    passAlongNewMaster.finalDestination = i;
                    
                    //Console.WriteLine("Node " + nodeID + " poking node " + i + " via node " + pathDictionary[i]);
                    SendPacket(passAlongNewMaster);
                }
            }

        }

        // Startup procedure.  This runs first on the hardware
        public void start()
        {
            if (RFNET.debugging)
            {
                Console.WriteLine("Node " + nodeID + " initializing...");
            }

            // Stop being offline
            RFNET.offlineNodeList.Remove(nodeID);  // SIM ONLY

            // World-wide remap
            WorldRemap();

        }

        // World-wide remap
        private void WorldRemap() // PORTED
        {
            // Map the network
            DoMap();


            // Gather a list of all nodes in the network (not including self)
            List<int> allNodes = pathDictionary.Keys.ToList();
            allNodes.Remove(nodeID);
            allNodes.Sort();

            // This cannot be random because poking will eventually poke this node again if it is not the lowest ID
            //Random rnd = new Random();
            int newMapVersion = nodeID;//rnd.Next(0, 255);

            // Set our own current map version, that way if a subsequent poke packet flies back through here, we don't process it.
            mapVersion = newMapVersion;

            // Process our own tree to be in sync with the others
            if(meshTree.Count > 0)
                ProcessTree();

            if (allNodes.Count > 0 && meshTree.Count > 0)
            {
                // Create a packet to send to the next node to map
                packet masterMapPacket = packetBuilder.CreateMasterPacket(nodeID, allNodes[0], meshTree, newMapVersion);

                // We will poke the lowest ID in the world (unless that's me???) TODO: Check what happens when lowest ID is the last to come online.  Appears to be ok in testing
                masterMapPacket.currentDestination = pathDictionary[allNodes[0]];
                SendPacket(masterMapPacket);
            }
            else  // Edge case: we are the first node online.  Mapping found nobody!  I'm so alone!!!
            {
                if (RFNET.debugging) { Console.WriteLine("I am the first node to come up or there are none within range."); }
            }
        }

        // Map network
        // PORTED
        public void DoMap()
        {
            // Add a FIFO queue to hold next vertecies to visit next
            Queue<int> vertexQueue = new Queue<int>();

            // To start, set me (node n) as the root of this tree of vertecies
            int rootVertex = nodeID;

            // Add the root of this tree to the queue
            vertexQueue.Enqueue(rootVertex);

            // Process the queue while it has elements
            while (vertexQueue.Count != 0)
            {
                //Console.WriteLine("Beginning pass through while; vertexQueue has " + vertexQueue.Count + " element(s)");

                // Pop an element off the queue
                int v = vertexQueue.Dequeue();
                //Console.WriteLine("Popped vertex ID " + v + " off the queue.");

                // Set current node (for sending packets from it)
                //Console.WriteLine("Currently processing node " + v);

                // If this is me doing the mapping, do it locally.  Otherwise request a remote mapping
                if (v == nodeID)
                {
                    // Send map packet to start getting a list of adjacent vertices             
                    packet mapPacket = packetBuilder.CreateDiscoverRequestPacket();
                    mapPacket.origin = nodeID;

                    // Get rid of previous neighbor list and pathDictionary 
                    // If this were a hardware node, this would be stored in volatile memory
                    // And cease to exist at poweroff, therefore we can't use this data at poweron
                    neighbors.Clear();      //SIM ONLY
                    pathDictionary.Clear(); //SIM ONLY

                    // Get neighbors list
                    SendPacket(mapPacket);

                    // Hardware implementation only -- listen for packets here
                    // CheckForPackets();

                    // Sort the neighbors
                    neighbors.Sort();

                    

                    // Add the neighbors to the queue
                    foreach (int i in neighbors)
                    {

                        if (!visited.Contains(i) && !vertexQueue.Contains(i))
                        {
                            vertexQueue.Enqueue(i);
                            if (RFNET.debugging) { Console.WriteLine("----> Enqueued " + i); }

                        }
                    }
                    visited.Add(nodeID);
                    if (RFNET.debugging)
                    {
                        Console.Write("Node " + nodeID + " has visited ");
                        foreach (int x in visited) { Console.Write(x + " "); }
                        Console.WriteLine();
                    }

                    pathDictionary[nodeID] = nodeID;
                }
                else // Remote mapping.  Type 3 request and type 4 response
                {
                    packet mapPacket = packetBuilder.CreateGetNeighborsPacket();
                    mapPacket.currentDestination = pathDictionary[v]; // Local lookup of next hop
                    mapPacket.finalDestination = v; // Final destination of this packet
                    mapPacket.origin = nodeID; // Me
                    mapPacket.previousDestination = nodeID;
                    SendPacket(mapPacket);

                    // Enqueue something in here or it breaks!!  Of course!
                    // Add the neighbors to the queue
                    foreach (int i in pathDictionary.Keys)
                    {
                        // If we have  not visited it and it is not already in the queue, add it
                        if (!visited.Contains(i) && !vertexQueue.Contains(i))
                        {
                            vertexQueue.Enqueue(i);
                            if (RFNET.debugging)
                            {
                                Console.WriteLine("----> Enqueued " + i);
                            }
                        }
                    }

                }

            }


            if (RFNET.debugging)
            {
                Console.WriteLine("----------------- Node " + nodeID + " has finished mapping.   ------------------------");
            }

            visited.Clear();
        }

        // Print neighbor map
        public void ShowPaths()
        {
            Console.WriteLine("\n--------- Paths for node " + nodeID + " ------------");
            List<int> keysFromPathDict = pathDictionary.Keys.ToList();
            keysFromPathDict.Sort();
            foreach (int key in keysFromPathDict)
            {
                Console.Write("Node " + key);
                Console.WriteLine(" via node " + pathDictionary[key]);
            }
        }

        // Heartbeat
        public void Heartbeat()
        {
            // Copy the current list of neighbors
            List<int> oldNeighbors = new List<int>(neighbors);

            // Clear the master list
            neighbors.Clear();

            // Rebuild the master list
            packet mapNeighbors = packetBuilder.CreateDiscoverRequestPacket();
            mapNeighbors.origin = nodeID;
            SendPacket(mapNeighbors);

            // Compare the lists
            if (!oldNeighbors.SequenceEqual(neighbors))
            {
                // Trigger world-wide remapping
                Console.WriteLine("\n==================== NEIGHBORS CHANGED: WORLDWIDE REMAP ====================");
                WorldRemap();
            }
            Console.WriteLine("\nHeartbeat: Success");

        }

        // Show meshTree
        public void ShowTree()
        {
            Console.WriteLine("---------- Mesh Tree -----------");
            Console.WriteLine("---- Format [parent, child] ----");
            foreach(int i in meshTree.Keys)
            {
                Console.Write("[" + i + ":");
                string nums = "";
                foreach(int j in meshTree[i])
                {
                    nums = nums + (j + ",");
                }
                nums = nums.TrimEnd(',');
                Console.WriteLine(nums + "]");
            }
            Console.WriteLine("--------------------------------");
        }

        // Process Tree
        public void ProcessTree(int treeID = -1)
        {
            if (treeID != -1)
            {
                if (RFNET.debugging) { Console.WriteLine("Node " + nodeID + " processing tree with ID " + treeID); }
            }
            else
            {
                treeID = nodeID;
                if (RFNET.debugging) { Console.WriteLine("Node " + nodeID + " processing an original map"); }
            }
            

            Dictionary<int, int> newPD = new Dictionary<int, int>();
            //List<int> mtkeys = meshTree.Keys.ToList<int>();

            //// Load mtkeys into newPD as keys
            //foreach(int i in mtkeys)
            //{
            //    newPD[i] = -1; // Load it with junk for now, just need to have keys in place.
            //}

            // Set self as self
            //newPD[nodeID] = nodeID;

            // Find self in tree (as a key) and add children as neighbors
            List<int> children = meshTree[nodeID];

            // Load the neighbors into the newPD
            foreach(int c in children)
            {
                newPD[c] = c; // Format for neighbors is FD = NH, ex. [5,5]
            }

            // Find grandchildren (children of neighbors)

            // Add a queue to store grandchildren in
            Queue<int> vertQueue = new Queue<int>();

            foreach(int c in children)
            {
                List<int> grandchildren = new List<int>(meshTree[c]);
                
                // Add all grandchildren for this child to the map
                foreach(int gc in grandchildren)
                {
                    if(!newPD.ContainsKey(gc) && !vertQueue.Contains(gc))
                    {
                        newPD[gc] = c;
                        vertQueue.Enqueue(gc);
                    }
                }
            }

            // Multihop

            while(vertQueue.Count > 0)
            {
                int currentV = vertQueue.Dequeue();

                foreach(int childOfQueued in meshTree[currentV])
                {
                    if (!newPD.ContainsKey(childOfQueued))
                    {
                        newPD[childOfQueued] = newPD[currentV];
                        vertQueue.Enqueue(childOfQueued);
                    }

                }

            }

            Debug.Assert(!newPD.Values.Contains(-1));

            Debug.Assert(newPD.Count == meshTree.Keys.Count);
            
            pathDictionary = newPD;

            mapVersion = treeID;

        }

        // Returns the best next hop to the <remoteNode>
        //private int FindHopForMultihop(int remoteNode, Dictionary<int, int> copyPD)
        //{
        //    int result = -1;

        //    List<int> parentsOfRemoteNode = meshTree[remoteNode];

        //    foreach(int i in parentsOfRemoteNode)
        //    {
        //        if(copyPD[i] != -1)
        //        {
        //            return copyPD[i];
        //        }
        //    }

        //    // No parents of <remoteNode> are known either (most common in distant mapping)
            

        //    return result;
        //}

        //private bool CheckChildrenForTarget(int parent, int target)
        //{
        //    if (meshTree[parent].Contains(target))
        //        return true;

        //    return false;
        //}


    }

    // THE packet object
    // PORTED
    public class packet
    {
        // packet type 1 = DiscoveryRequest, 2 = DiscoveryResponse, 3 = GetNeighbors, 4 = NeighborsList
        public int packetType;

        // Next hop
        public int currentDestination;

        // Last hop
        public int previousDestination;

        // Destination node
        public int finalDestination;

        // Origin node
        public int origin;

        // Packet data (check proper use of generic object?)
        public string payload;

    }

    // Creates various packet types (templates)
    // PORTED
    class packetBuilder
    {
        // Returns a DiscoveryRequest with a DiscoveryResponse
        public static packet CreateResponsePacket(packet discoveryRequest)
        {
            packet response = new packet();

            // Build responsePacket
            response.currentDestination = discoveryRequest.origin;
            response.finalDestination = discoveryRequest.origin;
            response.packetType = 2; // DiscoveryResponse packet type code
            response.payload = ""; // no payload necessary.  Any reponse to a DiscoverRequest is assumed to be from a neighbor.

            return response;
        }

        public static packet CreateDiscoverRequestPacket()
        {
            packet response = new packet();

            response.currentDestination = 255;
            response.finalDestination = 255;
            response.packetType = 1;

            return response;
        }

        public static packet CreateGetNeighborsPacket()
        {
            packet response = new packet();
            response.packetType = 3;
            response.payload = "";

            return response;
        }

        public static packet CreateNeighborsPacket()
        {
            packet response = new packet();
            response.packetType = 4;
            return response;
        }

        public static packet CreatePokePacket(int fromMe, int toHim)
        {
            packet response = new packet();
            response.packetType = 6;
            response.origin = fromMe;
            response.finalDestination = toHim;
            response.previousDestination = fromMe;
            return response;

        }

        // Simulator: these packet types aren't part of a physical implementation. Just here for sim/user interaction
        public static packet CreateOneWayPing(int dest)
        {
            packet response = new packet();
            response.packetType = 7;
            response.finalDestination = dest;
            return response;
        }

        public static packet CreateGeneralDataPacket(int dest, string payload)
        {
            packet response = new packet();
            response.packetType = 8;
            response.finalDestination = dest;
            response.payload = payload;
            return response;
        }

        internal static packet CreateMasterPacket(int fromMe, int toHim, Dictionary<int, List<int>> meshTree, int version)
        {
            packet response = new packet();
            response.packetType = 9;
            response.finalDestination = toHim;
            response.previousDestination = fromMe;
            response.origin = fromMe;
            

            // Construct a string with the network in it.
            string payload = "";

            foreach(int i in meshTree.Keys)
            {
                payload += i + ":";
                foreach(int j in meshTree[i])
                {
                    payload += j + ",";
                }
                payload.TrimEnd(',');
                payload += "|";
            }
            payload.TrimEnd('|');
            payload += "_" + version;
            response.payload = payload;

            return response;

        }
    }

    // Used to serialize the object and store it in XML.  // SIM ONLY
    public class formattedMeshNet
    {
        public List<node> contents;  // SIM ONLY
    }

    // Things related to the network itself  // ALL SIM ONLY
    class networkOps
    {
        //----------  NETWORK SIZE PARAMETERS --------------
        public static int maxNodes = 254;
        public static int minNodes = 254;
        public static int maxNeighbors = 3;
        //--------------------------------------------------

        public static List<node> meshNet = new List<node>(); // List of nodes for this session
        public static Dictionary<int, List<int>> hardCodedNeighbors = new Dictionary<int, List<int>>();

        // Random number
        public static Random rnd = new Random();
        public static int nodeCount = rnd.Next(minNodes, maxNodes + 1);

        // sometimes the generator attaches 6-8 nodes to one node, which doesn't look realistic on paper
        public static Dictionary<int, int> loadedNodes = new Dictionary<int, int>();

        // Keeps track of previous physNeighbors so we are sure to have bidirectional communication between nodes
        public static Dictionary<int, List<int>> thoseWhoSeeMe = new Dictionary<int, List<int>>();

        // Generates the network topology
        public static void GenerateNetwork(bool startAllOffline = false, bool lastOneOff = false)
        {

            // Generate random number of nodes
            for (int i = 1; i <= nodeCount; i++)
            {
                System.Threading.Thread.Sleep(5);
                node nextNode = new node();
                nextNode.nodeID = i;

                if (startAllOffline || (lastOneOff && i == nodeCount))
                {
                    RFNET.offlineNodeList.Add(i);
                }

                meshNet.Add(nextNode);

                Random rnd2 = new Random();
                int neighborCount = rnd2.Next(1, maxNeighbors + 1);

                // Adds a random number of physNeighbors to each node
                for (int k = 1; k <= neighborCount; k++)
                {
                    Random rndNeighbor = new Random();
                    int candidateNeighbor = rndNeighbor.Next(1, nodeCount + 1);

                    // Approval process ensures that our random numbers don't create a network where everything is connected to just one node
                    while (!ApproveNewNeighbor(candidateNeighbor, i, nextNode.physNeighbors))
                    {
                        candidateNeighbor = rndNeighbor.Next(1, nodeCount + 1);
                    }

                    nextNode.physNeighbors.Add(candidateNeighbor);

                    // Update the physNeighbors dictionary so we can fill in the blanks later
                    if (thoseWhoSeeMe.ContainsKey(candidateNeighbor))
                    {
                        thoseWhoSeeMe[candidateNeighbor].Add(nextNode.nodeID);
                    }
                    else
                    {
                        thoseWhoSeeMe[candidateNeighbor] = new List<int>();
                        thoseWhoSeeMe[candidateNeighbor].Add(nextNode.nodeID);
                    }

                    // Debug
                    //Console.Write("Node " + nextNode.nodeID + " can see " + candidateNeighbor);
                }
            }
            // Add the bidirectional nodes to each neighbor
            foreach (node n in meshNet)
            {
                foreach (node k in meshNet)
                {
                    if (k.physNeighbors.Contains(n.nodeID) && !n.physNeighbors.Contains(k.nodeID))
                    {
                        n.physNeighbors.Add(k.nodeID);
                    }
                }
            }
            PrintNetwork();
        }

        // Prints out a textual map of the network
        public static void PrintNetwork(bool showBothOnAndOff = true)
        {
            //Debug
            Console.WriteLine("Direct communication available for the following nodes");
            foreach (node n in meshNet)
            {
                if (showBothOnAndOff || !RFNET.offlineNodeList.Contains(n.nodeID))
                {
                    Console.Write("Node " + n.nodeID + " can see ");
                    n.physNeighbors.Sort();
                    foreach (int neighbor in n.physNeighbors)
                    {
                        Console.Write(neighbor + " ");
                    }
                    Console.WriteLine();
                }
            }


            Console.WriteLine("Your network has " + meshNet.Count + " nodes.  Network is not mapped.");
        }

        // Checks the accuracy of each node's map (length)
        public static void CheckValidity()
        {
            int failures = 0;
            foreach (node n in meshNet)
            {

                if (n.pathDictionary.Count != meshNet.Count)
                {
                    int individualPercent = 100 * n.pathDictionary.Count / (meshNet.Count);
                    Console.WriteLine("----> Node " + n.nodeID + " has only mapped " + n.pathDictionary.Count + " of " + (meshNet.Count) + " nodes. " +
                        individualPercent + "%  Map version: " + n.mapVersion);
                    failures++;

                }
                else
                {
                    if (RFNET.offlineNodeList.Contains(n.nodeID))
                    {
                        Console.WriteLine("----> Node " + n.nodeID + " is turned off");
                    }
                    else
                    {
                        Console.WriteLine("Node " + n.nodeID + " VALID --> map version " + n.mapVersion );
                    }
                }
            }
            int percent = 100 * (meshNet.Count - failures) / meshNet.Count;
            Console.WriteLine("Done: " + percent + "% correct");


            // Print out a check on map versions
            List<int> versions = new List<int>();

            foreach(node n in meshNet) { versions.Add(n.mapVersion); }

            if (versions.Distinct().Skip(1).Any())
            {
                Console.WriteLine("WARNING: Not all nodes are running the same map.");
            }
            else
            {
                Console.WriteLine("Success: all map versions are the same.");
            }

                if (percent < 100)
            {
                //Console.WriteLine("This is most likely due to the fact that the network generator has a small chance of generating a network " +
                //    "that actually has two seperate sections that have no path to connect to each other.  Basically, there are two or more networks " +
                //    " and thus nobody can get a complete map of all the other nodes.  Use the 'd' command to dump this network and 'g' to generate a new one" +
                //    ". If the problem persists, consider allowing more neighbors on the networkOps variable called maxNeighbors to reduce the liklihood of this issue.");
            }
            else
            {
                Console.WriteLine("\nInfo: Validation is done by comparing the number of paths available on each node to the number of nodes total.\nUse 'sho' to spot-check.\n");

            }
        }

        // Avoid duplicate physNeighbors
        public static bool ApproveNewNeighbor(int candidateNeighbor, int selfAddress, List<int> existingNeighbors)
        {

            if (loadedNodes.ContainsKey(candidateNeighbor))
            {
                if (loadedNodes[candidateNeighbor] >= 3)
                {
                    return false;
                }
            }
            else
            {
                loadedNodes[candidateNeighbor] = 0;
            }


            if (!existingNeighbors.Contains(candidateNeighbor) && candidateNeighbor != selfAddress)
            {
                loadedNodes[candidateNeighbor]++;
                return true;

            }

            return false;
        }

        // Saves the current network for later use
        internal static void SaveNetwork()
        {
            // Copy the meshNet into the formattedMeshNet object
            formattedMeshNet saveMe = new formattedMeshNet();
            saveMe.contents = meshNet;

            foreach (node n in saveMe.contents)
            {
                n.pathDictionary = new Dictionary<int, int>();
            }

            // Create an XML writer
            XmlSerializer writer = new XmlSerializer(typeof(formattedMeshNet));

            var path = /*Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\*/"netTransform.xml";
            var transformPath = /*Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\*/"GraphFormatTransform.xsl";
            var savePath = /*Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\*/"ExportedNetwork.graphml";

            System.IO.FileStream file = System.IO.File.Create(path);

            writer.Serialize(file, saveMe);
            file.Close();

            // Add style sheet association
            XmlDocument xDoc = new XmlDocument();
            var transformAssn = xDoc.CreateProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"GraphFormatTransform.xsl\" version=\"1.0\"");
            xDoc.AppendChild(transformAssn);


            // Load xml just saved
            System.Xml.Xsl.XslCompiledTransform myXslTransform;

            myXslTransform = new System.Xml.Xsl.XslCompiledTransform();

            myXslTransform.Load(transformPath);

            XmlWriterSettings xws = myXslTransform.OutputSettings.Clone();
            xws.Encoding = Encoding.UTF8;
            xws.Encoding = new UTF8Encoding(false);


            using (XmlWriter xw = XmlWriter.Create(savePath, xws))
            {
                myXslTransform.Transform(path, xw);
            }




        }

        // Loads a previously saved network topology
        internal static void LoadNetwork()
        {
            // Load the mesh from the file
            XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(formattedMeshNet));
            var path = /*Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + */"netTransform.xml";
            System.IO.StreamReader file = new System.IO.StreamReader(path);

            formattedMeshNet readFromFile = (formattedMeshNet)reader.Deserialize(file);
            file.Close();

            meshNet = readFromFile.contents;

        }

        // Turn off all nodes simultaneously
        internal static void AllOff()
        {
            foreach (node n in meshNet)
            {
                RFNET.offlineNodeList.Add(n.nodeID);
            }
        }

        // Turns on all nodes in random order
        internal static void randomOn()
        {
            RFNET.offlineNodeList.Sort();

            while (RFNET.offlineNodeList.Count > 0)
            {
                int nextNode = randomOfflineNode() - 1;

                // Start a timer to see how long it takes to add this node to the network
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Start a node
                meshNet[nextNode].start();

                // Stop the map timer
                watch.Stop();

                // Calculate and format the time that took
                long elapsedms = watch.ElapsedMilliseconds;
                RFNET.secondsToMap += elapsedms;

                Console.WriteLine(/*"Turned on node " + (nextNode + 1) + " in " +*/ elapsedms /*+ "ms"*/);
            }
            Console.WriteLine("Procedure took a total of " + RFNET.secondsToMap / 1000 + "s");
            RFNET.secondsToMap = 0;
        }

        // Randomly selects an offline node
        private static int randomOfflineNode()
        {
            Random rand = new Random();
            return RFNET.offlineNodeList[rand.Next(0, RFNET.offlineNodeList.Count)];
        }

        // Randomly selects an online node
        private static int randomOnlineNode()
        {
            Random rand = new Random();
            List<int> targetNodes = new List<int>();

            foreach (node n in meshNet)
            {
                targetNodes.Add(n.nodeID);
            }

            foreach (int i in RFNET.offlineNodeList)
            {
                targetNodes.Remove(i);
            }

            return targetNodes[rand.Next(0, targetNodes.Count)];

        }

        // Start each node in order (note: this uses the optimal time delay between power-ups)
        internal static void sequentialOn()
        {
            foreach (node n in meshNet)
            {
                // Start a timer to see how long it takes to add this node to the network
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Start a node
                n.start();

                // Calculate and format the time that took
                long elapsedms = watch.ElapsedMilliseconds;
                RFNET.secondsToMap += elapsedms;

                Console.WriteLine("Turned on node " + (n.nodeID) + " in " + elapsedms + "ms");
            }
            Console.WriteLine("Operation took " + RFNET.secondsToMap / 1000 + "s to complete");
            RFNET.secondsToMap = 0;
        }

        // Starts a heartbeat on all online nodes in random order
        internal static void heartbeatAll()
        {
            List<int> alreadyBeat = new List<int>();

            while (alreadyBeat.Count < (meshNet.Count - RFNET.offlineNodeList.Count))
            {
                int randomNode = randomOnlineNode();

                if (!alreadyBeat.Contains(randomNode))
                {
                    meshNet[randomNode - 1].Heartbeat();
                    alreadyBeat.Add(randomNode);
                }
            }

        }

        // Takes a random node offline
        internal static void killRandom()
        {
            int target = randomOnlineNode();
            Console.WriteLine("Taking " + target + " offline.");
            RFNET.offlineNodeList.Add(target);
        }
    }
}
