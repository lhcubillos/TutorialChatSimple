using Foundation;
using System;
using UIKit;
using AddressBook;

namespace ChatSimple
{
	partial class ContactosViewController : UIViewController
	{
		//We use this variable to pass information on segues.
		public ABPerson contactToPass;

		//Mandatory line, to use this view controller on the storyboard.
		public ContactosViewController (IntPtr handle) : base (handle)
		{
		}

public override void ViewDidLoad ()
{
	base.ViewDidLoad ();

	//We give the table on the storyboard some data.
	var tableSource = new ContactosTableSource ();
	Contacts.ReloadData ();
	tableSource.controller = this;
	//Our tableview name is Contacs, and we pass it the source we created.
	Contacts.Source = tableSource;
}

		/// <summary>
		/// Prepares for segue, and passes the important information with the contactToPass variable.
		/// </summary>
		/// <param name="segue">Segue.</param>
		/// <param name="sender">Sender.</param>
		public override void PrepareForSegue (UIStoryboardSegue segue, NSObject sender)
		{
			base.PrepareForSegue (segue, sender);
			(segue.DestinationViewController as ChatViewController).abContact = this.contactToPass;
		}
	}
}
