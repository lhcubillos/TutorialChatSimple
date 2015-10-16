using System;
using UIKit;
using AddressBook;
using System.Collections.Generic;
using System.Linq;
using Foundation;

namespace ChatSimple
{
	public class ContactosTableSource:UITableViewSource
	{
		//We declare the dictionary we are going to use.
		Dictionary<string,List<ABPerson>> contacts;
		//Last name's initials, for the index
		List<string> initials = new List<string>();

		public static string cellIdentifier = "Contact";
		public UIViewController controller;

		public ContactosTableSource ()
		{
			GetAddressBook ();	
		}

		//Fills contacts and initials with the data on the phone.
		public void GetAddressBook()
		{
			NSError err;
			var addressBook = ABAddressBook.Create (out err);
			//The user must agree to let us use his contacts.
			addressBook.RequestAccess ((bool haveAccess, NSError e) => {
				if (!haveAccess) {
					Console.WriteLine ("No access, you must accept to use the app");
				} else{
					Console.WriteLine ("Access granted");
				}
			});

			contacts = new Dictionary<string, List<ABPerson>> ();
			var persons = addressBook.GetPeople ();

			foreach (ABPerson person in persons) {
				string firstLetterLastName;
				if (person.FirstName != null && person.LastName != null)
					firstLetterLastName = person.LastName [0].ToString ().ToUpper ();
				else if (person.FirstName != null)
					firstLetterLastName = person.FirstName [0].ToString ().ToUpper ();
				else
					continue;
				if (contacts.ContainsKey (firstLetterLastName))
					contacts [firstLetterLastName].Add (person);
				else {
					contacts.Add (firstLetterLastName, new List<ABPerson> ());
					contacts [firstLetterLastName].Add (person);
				}
			}

			foreach (KeyValuePair<string,List<ABPerson>> kp in contacts) {
				initials.Add (kp.Key);
				kp.Value.OrderByDescending (persona => persona.LastName);
			}
			initials.Sort ();
		}

		/// <summary>
		/// Lets us determine cell information, given its index path.
		/// </summary>
		/// <returns>The cell.</returns>
		/// <param name="tableView">Table view.</param>
		/// <param name="indexPath">Index path.</param>
		public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
		{
			//This code snippet allows us to reuse a cell to show content. iOS tables work this way, there is a fixed number of cells, and
			//you have to reuse the ones that are not on the user interface.
			UITableViewCell cell = tableView.DequeueReusableCell (cellIdentifier);
			if (cell == null) 
				cell = new UITableViewCell (UITableViewCellStyle.Default, cellIdentifier);

			//Cell content.
			var person = contacts [initials [indexPath.Section]] [indexPath.Row];
			cell.TextLabel.Text = person.FirstName +" "+ person.LastName;
			return cell;
		}

		/// <summary>
		/// Returns the number of rows per section.
		/// </summary>
		/// <returns>The in section.</returns>
		/// <param name="tableview">Tableview.</param>
		/// <param name="section">Section.</param>
		public override nint RowsInSection (UITableView tableview, nint section)
		{
			return (contacts [(initials.ToArray()) [section]].ToArray()).Length;
		}

		/// <summary>
		/// Returns the number of sections.
		/// </summary>
		/// <returns>The of sections.</returns>
		/// <param name="tableView">Table view.</param>
		public override nint NumberOfSections (UITableView tableView)
		{
			return initials.Count;
		}

		/// <summary>
		/// Returns the titles for the sections.
		/// </summary>
		/// <returns>The for header.</returns>
		/// <param name="tableView">Table view.</param>
		/// <param name="section">Section.</param>
		public override string TitleForHeader (UITableView tableView, nint section)
		{
			return (initials.ToArray()) [section];
		}

		/// <summary>
		/// Last name initials.
		/// </summary>
		/// <returns>The index titles.</returns>
		/// <param name="tableView">Table view.</param>
		public override string[] SectionIndexTitles (UITableView tableView)
		{
			return initials.ToArray ();
		}

		/// <summary>
		/// Handles a row selected. We direct the user to the chat view controller, and pass the relevant information.
		/// </summary>
		/// <param name="tableView">Table view.</param>
		/// <param name="indexPath">Index path.</param>
		public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
			var person = contacts [initials [indexPath.Section]] [indexPath.Row];
			(controller as ContactosViewController).contactToPass = person;
			controller.PerformSegue ("ToChat", controller);

			tableView.DeselectRow (indexPath, false);
		}

	}
}

