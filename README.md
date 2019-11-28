# Hakeem/TestBot
## Hosting and Structure
* Written in C# using the Microsoft Bot framework
* Published to Azure where it is a bot Channels registration (Type of resource)
* Deployed using the Skype channel through the portal
* Backed up to git – NetHope repository
  * Master branch is published to Hakeem (NetHopePass bot channel registration)
  * Development Branch is published to test slot (TestSlotNetHope bot channel registration)
* Test bot uses the conversation portal when a user says hello (V2 bot – reads from MongoDB conversation tree)
* V1 bot interacts with an API for a large portion of its features
* V1 bot interacts with LUIS (ultimately so will the V2 bot)
* Subscription is now the Microsoft Azure Sponsorship subscription
* Can’t use Chrome to log in to Azure (I use Firefox)

To publish the bot from Visual Studio to the Azure Channel, right click on the the bot application in the solution explorer and click publish. If the bot channel is not configured, select new profile and fill in the information with the endpoints found in the settings page of the bot channels registration in the Azure portal. The password and AppID are also in the web.config file of the solution. 
*As the development and master branches are published to different channels, they each have different passwords and AppIDs in the web.config, and are pushed to different Azure profiles*

As a starting point, please find a useful tutorial here: https://tutorials.botsfloor.com/creating-a-bot-using-c-and-microsoft-bot-framework-a344420f9d6f

## Features to note in the code – some things not immediately evident in the code
* the message controller decides what to do with the incoming activity depending on its type (message, contactRelationUpdate, etc.)
* messages are sent to the RootDialog.
For V2 conversation, the bot sources the conversation from the database, for V1 bots, LUIS is used to determine the type of input, and react accordingly. 
*	As well as normal dialog flow, scorables are in the scorable folder. They check for specific input matches, and then call a dialog if they match (E.g. tts, feedback, etc.)
*	User can speak to the bot and the bot will reply with audio
*	The user can say tts and the bot will repeat the last message as audio
* user speech is not stored , it is transcribed and disposed
* bot produced audio is stored in blob storage (audiofilestore in Azure, in the mycontainer container). This is because the user can only play the audio for as long as it exists physically somewhere
*	The bot stores a stack of the conversation between the bot and the user 
  *	both user input and bot output stored
*	Each time the user interacts with the bot the MongoDB is updated with the time of interaction of that user
  *	This is so we can send proactive messages at intervals 
*	The user language is detected on input. The user preferences are also saved through the preferences command.
  *	TODO, change the bot language depending on the user input language (which is already detected and saved to a user variables) and isn’t dependent on the user preference.
*	Proactive messages
*	We need to make language not a parameter that we ask about, but a parameter that decides what courses are shown






