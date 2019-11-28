using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Newtonsoft.Json;

namespace NetHope.Dialogs
{
    [Serializable]
    public class MongoRead : IDialog<object>

    {
        private static readonly HttpClient CLIENT = new HttpClient();
        private static readonly IMongoCollection<Node> collection = GetReferenceToCollection("conversation_trees");
        private Node currentNode;
        private static readonly Regex re = new Regex(@"\{([^\}]+)\}");
        private static readonly string reString = @"\{([^\}]+)\}";
        private string userDataVariable;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        // Receives an activity of suggestions from page or dictionary
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            Node root = FindRoot()[0];
            await PresentNode(root, context);
        }

        private async Task PresentNode(Node node, IDialogContext context)
        {
            /* Displays the contents of the node */

            List<Node> children = await ReferenceToChildNodes(node, collection); // get all the children of the current node
            if (node.node_type == "bot")
            {
                var response = context.MakeMessage();
                response.Text = await AddVariables(node.text, context); // we swap $variables for their values
                string child_type = children[0].node_type; //get nodetype of first node

                // Case 1: The node has a freeform user variables as next input 
                if (child_type == "user" && children[0].user_input == true)
                {
                    try
                    {
                        userDataVariable = children[0].text;
                        currentNode = children[0];
                        await context.PostAsync(response);
                        context.Wait(ListenForUserVar);
                    }
                    catch (Exception e)
                    {
                        await context.PostAsync(e.ToString());
                    }
                }

                // Case 2: The node has suggestions as next input
                else if (child_type == "user" && children[0].user_input == false)
                {
                    List<CardAction> childSuggestions = new List<CardAction>();
                    foreach (Node child in children)
                    {
                        childSuggestions.Add(new CardAction() { Title = child.text, Type = ActionTypes.PostBack, Value = child.Id });
                    }
                    response.SuggestedActions = new SuggestedActions()
                    {
                        Actions = childSuggestions
                    };
                    await context.PostAsync(response);
                    context.Wait(SuggetionSelected);
                }

                // Case 3: The node has one child, and its a bot response not a user input
                else if (child_type == "bot")
                {
                    await context.PostAsync(response);
                }
                else
                {
                    await context.PostAsync("in the else");
                }
            }
            else
            {
                if (children.Count() > 0) // check if children of the node exist
                {
                    await PresentNode(children[0], context); // calls the method again
                }
                else
                {
                    await context.PostAsync("end of tree"); // reached the end of tree
                }
            }
        }

        private async Task SuggetionSelected(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            string nodeId = activity.Text;
            //await context.PostAsync(selected.text);
            Node node = NodeById(nodeId);
            //await context.PostAsync(node.ToString());
            await PresentNode(node, context);
        }

        /* Method that is called when a freeform user entry is expected to save it to a uservariable */
        private async Task ListenForUserVar(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            string text = activity.Text;
            context.UserData.SetValue(userDataVariable, text);
            //userDataVariable = "";
            //context.UserData.TryGetValue<Node>("node", out Node node);
            await context.PostAsync("the variable is " + userDataVariable + " and the value is " + text);
            await PresentNode(currentNode, context);
        }

        /* Method that takes the name of a collection as a parameter and returns a reference to that collection */
        public static IMongoCollection<Node> GetReferenceToCollection(string collectionName)
        {
            var client = new MongoClient(
            new MongoClientSettings
            {
                Credential = MongoCredential.CreateCredential("mydb", "jake", "running")
             ,
                Server = new MongoServerAddress("13.81.172.78", 27017)
            });
            IMongoDatabase _database = client.GetDatabase("mydb");
            return _database.GetCollection<Node>(collectionName);
        }

        /* Method that searches for a node with no parents and returns it as the root */
        public List<Node> FindRoot()
        {
            List<Node> results = collection.Find(x => x.parents.Length == 0).ToList();
            return results;
        }

        public Node NodeById(string Id)
        {
            Node results = collection.Find(x => x.Id == new ObjectId(Id)).FirstOrDefault();
            return results;
        }

        /* Method returns a list of nodes that are the child of a given node */
        public async Task<List<Node>> ReferenceToChildNodes(Node node, IMongoCollection<Node> collection)
        {
            List<Node> childNodes = new List<Node>();
            foreach (ObjectId y in node.children)
            {
                childNodes.Add(collection.Find(x => x.Id == y).First());
            }
            return childNodes;
        }

        /* Method searches the string for {} symboling that a variable needs to be inserted and makes the insertion */
        public async Task<string> AddVariables(string text, IDialogContext context)
        {
            while (text.Contains("{") && text.Contains("}"))
            {
                string keyName = Regex.Match(text, reString).Groups[1].Value;
                await context.PostAsync(keyName);
                try
                {
                    text = re.Replace(text, context.UserData.GetValue<string>(keyName), 1);
                }
                catch (KeyNotFoundException)
                {
                }
            }
            return text;
        }
    }

    [Serializable]
    public class Node
    {
        public ObjectId Id { get; set; }
        public ObjectId[] parents { get; set; }
        public ObjectId[] children { get; set; }
        public string node_type { get; set; }
        public string text { get; set; }
        public string author { get; set; }
        public bool user_input { get; set; }

        override
        public string ToString()
        {
            string type = user_input ? "user variable: " : "user text: ";
            return type + text;
        }
    }
}