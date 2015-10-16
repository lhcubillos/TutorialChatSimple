using System;
using UIKit;
using Foundation;
using JSQMessagesViewController;
using System.Collections.Generic;
using System.Threading.Tasks;
using AddressBook;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading;


namespace ChatSimple
{
	/// <summary>
	/// The user on the server, consisting of a name and a phone number.
	/// </summary>
	public class User
	{
		public string id { get; set; }
		public string phoneNumber { get; set;}
		public string name { get; set;}
	}

	/// <summary>
	/// Class to handle the messages from and to the server.
	/// </summary>
	public class MessageServer
	{
		public string sender{ get; set;}
		public string content{ get; set; }
		public string conversation_id{ get; set; }

		//These two methods are used to compare two messages by the sender and their content.
		public override bool Equals(object obj)
		{
			var other = obj as MessageServer;
			if (other == null)
				return false;
			return (this.sender == other.sender && this.content == other.content);
		}
		public override int GetHashCode()
		{
			return sender.GetHashCode() ^ content.GetHashCode();
		}
	}

	/// <summary>
	/// The registered user on the API.
	/// </summary>
	public class APIUser
	{
		public string phoneNumber{ get; set; }
		public string password{ get; set; }
		public string name{ get; set; }
	}

	/// <summary>
	/// A conversation of only two people.
	/// </summary>
	public class TwoConversation
	{
		public string first{ get; set; }
		public string second{ get; set; }
		public string title{ get; set; }
	}

	/// <summary>
	/// Class to handle the response of a request creating a two conversation.
	/// </summary>
	public class TwoConversationAnswer
	{
		public string id{ get; set;}
		public bool group{ get; set;}
	}

	//This line is MANDATORY, without it you can't use this controller on the storyboard.
	[Register ("ChatViewController")]
	public class ChatViewController : MessagesViewController
	{
		const string myPhoneNumber = "56983362592";
		const string token = "UAGIw55wqzjwzLKLuVVHvwtt";
		const string myName = "Luis";

		MessagesBubbleImage outgoingBubbleImageData, incomingBubbleImageData;
		public ABPerson abContact;
		User contact;
		List<MessageServer> messages = new List<MessageServer>();
		string idConversation;

		//This line is also mandatory to allow the use of this class on the storyboard.
		public ChatViewController(IntPtr handle):base(handle)
		{
		}

		/// <summary>
		/// Starts the thread that will create a conversation if there is none, or get the conversation that exists.
		/// </summary>
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			contact = new User () {
				phoneNumber = abContact.GetPhones ().GetValues() [0],
				name = (abContact.FirstName +" "+ abContact.LastName)
			};

			//We start the thread. It won't run forever.
			var ThreadRequest = new Thread(DoRequest);
			ThreadRequest.Start ();

			Title = contact.name;

			// You must set your senderId and display name
			SenderId = myPhoneNumber;
			SenderDisplayName = myName;

			// These MessagesBubbleImages will be used in the GetMessageBubbleImageData override
			var bubbleFactory = new MessagesBubbleImageFactory ();
			outgoingBubbleImageData = bubbleFactory.CreateOutgoingMessagesBubbleImage (UIColorExtensions.MessageBubbleLightGrayColor);
			incomingBubbleImageData = bubbleFactory.CreateIncomingMessagesBubbleImage (UIColorExtensions.MessageBubbleBlueColor);

			// Remove the Avatars
			CollectionView.CollectionViewLayout.IncomingAvatarViewSize = CoreGraphics.CGSize.Empty;
			CollectionView.CollectionViewLayout.OutgoingAvatarViewSize = CoreGraphics.CGSize.Empty;


		}

		/// <summary>
		/// Method to modify the cell of each message given its index path.
		/// </summary>
		/// <returns>The cell.</returns>
		/// <param name="collectionView">Collection view.</param>
		/// <param name="indexPath">Index path.</param>
		public override UICollectionViewCell GetCell (UICollectionView collectionView, NSIndexPath indexPath) 
		{
			var cell =  base.GetCell (collectionView, indexPath) as MessagesCollectionViewCell;

			// Override GetCell to make modifications to the cell
			// In this case darken the text for the sender
			var message = messages [indexPath.Row];
			if (message.sender == SenderId)
				cell.TextView.TextColor = UIColor.Black;

			return cell;
		}

		/// <summary>
		/// Gets the number of messages on the conversation.
		/// </summary>
		/// <returns>The items count.</returns>
		/// <param name="collectionView">Collection view.</param>
		/// <param name="section">Section.</param>
		public override nint GetItemsCount (UICollectionView collectionView, nint section)
		{
			return messages.Count;
		}

		/// <summary>
		/// Gets the message data.
		/// </summary>
		/// <returns>The message data.</returns>
		/// <param name="collectionView">Collection view.</param>
		/// <param name="indexPath">Index path.</param>
		public override IMessageData GetMessageData (MessagesCollectionView collectionView, NSIndexPath indexPath)
		{
			var message = messages [indexPath.Row];
			var msg = new Message (message.sender, ContactosTableSource.cellIdentifier, NSDate.Now, message.content);
			return msg;
		}

		/// <summary>
		/// Gets the message bubble image data.
		/// </summary>
		/// <returns>The message bubble image data.</returns>
		/// <param name="collectionView">Collection view.</param>
		/// <param name="indexPath">Index path.</param>
		public override IMessageBubbleImageDataSource GetMessageBubbleImageData (MessagesCollectionView collectionView, NSIndexPath indexPath)
		{
			var message = messages [indexPath.Row];
			if (message.sender == SenderId)
				return outgoingBubbleImageData;
			return incomingBubbleImageData;
		}

		//We are not using this feature, so we set it to null.
		public override IMessageAvatarImageDataSource GetAvatarImageData (MessagesCollectionView collectionView, NSIndexPath indexPath)
		{
			return null;
		}

		/// <summary>
		/// Handles the send button pressed event. We send the message to the user or users on the conversation.
		/// </summary>
		/// <param name="button">Button.</param>
		/// <param name="text">Text.</param>
		/// <param name="senderId">Sender identifier.</param>
		/// <param name="senderDisplayName">Sender display name.</param>
		/// <param name="date">Date.</param>
		public override async void PressedSendButton (UIButton button, string text, string senderId, string senderDisplayName, NSDate date)
		{
			await SendMessage (text);
		}

		/// <summary>
		/// Determines whether the contact is or not a user on the API.
		/// </summary>
		/// <returns><c>true</c> if this instance is user; otherwise, <c>false</c>.</returns>
		public bool IsUser()
		{
			//request URL
			string URL = "http://guasapuc.herokuapp.com/users.json";
			try 
			{
				var request = WebRequest.Create(URL) as HttpWebRequest;
				request.Method = "GET"; //request type
				//Headers
				request.ContentType = "application/json";
				request.Headers.Add("Authorization", "Token token="+token); //safety measure on the API

				//The answer to the request.
				HttpWebResponse Httpresponse = (HttpWebResponse)request.GetResponse();
				StreamReader sr = new StreamReader (Httpresponse.GetResponseStream ());
				string answerJson = sr.ReadToEnd ();
				sr.Close (); //We close the reader.

				//We deserialize the users already on the API
				var users = JsonConvert.DeserializeObject<List<APIUser>>(answerJson);

				//Check if the contact is or isn't there.
				foreach(APIUser u in users){
					if (u.phoneNumber == this.contact.phoneNumber)
						return true;
				}
				return false;
			}
			catch(Exception e)
			{
				UIAlertView error = new UIAlertView ("Error", e.Message, null, "Ok", null);
				error.Show ();
				return false;
			}
		}

		/// <summary>
		/// Adds the user to the API.
		/// </summary>
		public void AddUser()
		{
			//Request URL.
			string URL = "http://guasapuc.herokuapp.com/api/v2/users";
			try 
			{
				var request = WebRequest.Create(URL) as HttpWebRequest;
				request.Method = "POST";

				//same headers as on GET method.
				request.ContentType = "application/json";
				request.Headers.Add("Authorization", "Token token="+token);

				//Instantiate the stream writer, in charge of modifying the data on the server.
				var sw = new StreamWriter(request.GetRequestStream());

				//We create the contact and serialize it to send it to the server
				var user = new APIUser(){phoneNumber=contact.phoneNumber, name=contact.name, password="123456"};
				string json = JsonConvert.SerializeObject(user);
				//the new user is written on the API
				sw.Write(json);
				sw.Flush();
				sw.Close(); //The writer is shut.

				//Necessary to read the full response.
				HttpWebResponse Httpresponse = (HttpWebResponse)request.GetResponse();
				StreamReader sr = new StreamReader (Httpresponse.GetResponseStream ());
				sr.ReadToEnd ();
				sr.Close ();
			}
			catch(Exception e)
			{
				//Captures errors.
				Console.WriteLine("Error: "+e.Message);
			}
		}



		/// <summary>
		/// Sends the message asynchronously through a request.
		/// </summary>
		/// <returns>The message.</returns>
		/// <param name="text">Text.</param>
		async Task SendMessage(string text)
		{
			ScrollToBottom (true);
			var m = new MessageServer(){content = text, sender = myPhoneNumber, conversation_id =idConversation};
			messages.Add(m);
			FinishSendingMessage(true);
			SystemSoundPlayer.PlayMessageSentSound ();

			string URL = "http://guasapuc.herokuapp.com/api/v2/conversations/send_message";
			try 
			{
				var request = WebRequest.Create(URL) as HttpWebRequest;
				request.Method = "POST";
				request.ContentType = "application/json";
				request.Headers.Add("Authorization", "Token token="+token);
				var sw = new StreamWriter(request.GetRequestStream());

				string json = JsonConvert.SerializeObject(m);
				sw.Write(json);
				sw.Flush();
				sw.Close();

				HttpWebResponse Httpresponse = (HttpWebResponse)request.GetResponse();
				StreamReader sr = new StreamReader (Httpresponse.GetResponseStream ());
				string answerJson = sr.ReadToEnd ();
				sr.Close ();
			}
			catch(Exception e)
			{
				Console.WriteLine("Error: "+e.Message);
			}

			ScrollToBottom (true);
		}

		/// <summary>
		/// Handles the accesory button pressed event. It shows an action sheet to refresh the messages on the conversation.
		/// </summary>
		/// <param name="sender">Sender.</param>
		public override void PressedAccessoryButton (UIButton sender)
		{
			base.PressedAccessoryButton (sender);

			//A button to refresh.
			var refreshMessages = new Action<UIAlertAction> (RefreshMessages);
			var alerta = UIAlertController.Create(null, null,UIAlertControllerStyle.ActionSheet);
			alerta.AddAction(UIAlertAction.Create("Refresh Messages", UIAlertActionStyle.Default, refreshMessages));
			//Cancel button.
			alerta.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null));
			//Shows the alert.
			PresentViewController (alerta, true, null);
		}

		/// <summary>
		/// Gets all messages from server, and puts on the conversation those that aren't already there.
		/// </summary>
		/// <param name="obj">Object.</param>
		void RefreshMessages(UIAlertAction obj)
		{
			var messagesConversation = GetMessagesConversation ();
			foreach (MessageServer m in messagesConversation) {
				if (!messages.Contains (m))
					messages.Add (m);
			}
			FinishReceivingMessage (true);
			ScrollToBottom (true);
		}

		/// <summary>
		/// Gets messages from server of the conversation I'm on.
		/// </summary>
		/// <returns>The messages conversation.</returns>
		public List<MessageServer> GetMessagesConversation()
		{
			string URL = "http://guasapuc.herokuapp.com/api/v2/conversations/get_messages?conversation_id="+idConversation;
			try 
			{
				var request = WebRequest.Create(URL) as HttpWebRequest;
				request.Method = "GET";
				request.ContentType = "application/json";
				request.Headers.Add("Authorization", "Token token="+token);

				HttpWebResponse Httpresponse = (HttpWebResponse)request.GetResponse();
				StreamReader sr = new StreamReader (Httpresponse.GetResponseStream ());
				string answerJson = sr.ReadToEnd ();
				sr.Close ();
				var messagesServer = JsonConvert.DeserializeObject<List<MessageServer>>(answerJson);
				return messagesServer;
			}
			catch(Exception e)
			{
				Console.WriteLine("Error: "+e.Message);
				return null;
			}
		}


		/// <summary>
		/// Checks if the user is already on the API. If he is, we only create the conversation. If he isn't, we add him, and then
		/// create the conversation. If the conversation exists, we get the id of it.
		/// </summary>
		public void DoRequest()
		{
			if (!IsUser ()) 
			{
				AddUser ();
				CreateConversation ();
			} 
			else 
			{
				if (!WeHaveConversation ()) 
				{
					CreateConversation ();
				}
			}
		}

		/// <summary>
		/// Creates a conversation with the user.
		/// </summary>
		public void CreateConversation()
		{
			string URL = "http://guasapuc.herokuapp.com/api/v2/conversations/create_two_conversation";
			try 
			{
				var request = WebRequest.Create(URL) as HttpWebRequest;
				request.Method = "POST";
				request.ContentType = "application/json";
				request.Headers.Add("Authorization", "Token token="+token);
				var sw = new StreamWriter(request.GetRequestStream());

				var m = new TwoConversation(){first=contact.phoneNumber, second=myPhoneNumber, title=contact.name};
				string json = JsonConvert.SerializeObject(m);
				Console.WriteLine("Conversacion a crear: "+json);
				sw.Write(json);
				sw.Flush();
				sw.Close();

				//Es necesario leer la respuesta
				HttpWebResponse Httpresponse = (HttpWebResponse)request.GetResponse();
				StreamReader sr = new StreamReader (Httpresponse.GetResponseStream ());
				string jsonRespuesta = sr.ReadToEnd ();
				idConversation = JsonConvert.DeserializeObject<TwoConversationAnswer>(jsonRespuesta).id;
				sr.Close ();
			}
			catch(Exception e)
			{
				Console.WriteLine("Error: "+e.Message);		
			}
		}

		/// <summary>
		/// Determines if we already have a conversation with the user or not.
		/// </summary>
		/// <returns><c>true</c>, if have conversation was wed, <c>false</c> otherwise.</returns>
		public bool WeHaveConversation()
		{
			var contactConversations = GetUserConversations (contact.phoneNumber);
			var myConversations = GetUserConversations (myPhoneNumber);

			foreach (string id in myConversations) 
			{
				if (contactConversations.Contains (id)) 
				{
					idConversation = id;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets all conversations of a user given his phone number.
		/// </summary>
		/// <returns>The user conversations.</returns>
		/// <param name="userNumber">User number.</param>
		public List<string> GetUserConversations(string userNumber)
		{
			string URL = "http://guasapuc.herokuapp.com/api/v2/users/get_conversations?phone_number="+userNumber;
			try 
			{
				var request = WebRequest.Create(URL) as HttpWebRequest;
				request.Method = "GET";
				request.ContentType = "application/json";
				request.Headers.Add("Authorization", "Token token="+token);

				HttpWebResponse Httpresponse = (HttpWebResponse)request.GetResponse();
				StreamReader sr = new StreamReader (Httpresponse.GetResponseStream ());
				string jsonRespuesta = sr.ReadToEnd ();
				sr.Close ();
				var conversations = JsonConvert.DeserializeObject<List<TwoConversationAnswer>>(jsonRespuesta);
				List<string> ids = new List<string>();
				foreach (TwoConversationAnswer conv in conversations)
					ids.Add(conv.id);
				return ids;
			}
			catch(Exception e)
			{
				Console.WriteLine("Error: "+e.Message);			
				return null;
			}
		}
	}
}