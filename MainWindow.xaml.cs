using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Practise_;
using System.ComponentModel;

namespace Practise
{
    public partial class MainWindow : Window
    {
        string server = "smtp.gmail.com";
        int port = 587;
        const string username = "semenkos2005@gmail.com"; // Replace with your actual email
        const string password = "ogur feiz fcam vglq"; // Replace with your actual password
        private ImapClient client;

        public MainWindow()
        {
            InitializeComponent();
            client = new ImapClient();
            DisableInputFields();
            mailServiceComboBox.Items.Add("Gmail");
            mailServiceComboBox.Items.Add("Outlook");
            mailServiceComboBox.Items.Add("Yahoo");
        }
        private void DisableInputFields()
        {
            folderListBox.IsEnabled = false;
            messageListBox.IsEnabled = false;
            fromTextBlock.IsEnabled = false;
            toTextBlock.IsEnabled = false;
            subjectTextBlock.IsEnabled = false;
            bodyTextBox.IsEnabled = false;
            sendButton.IsEnabled = false;
            attachButton.IsEnabled = false;
        }
        private void EnableInputFields()
        {
            mailServiceComboBox.IsEnabled = false;
            emailTextBox.IsEnabled = false;
            passwordBox.IsEnabled = false;
            loginButton.IsEnabled = false;
            folderListBox.IsEnabled = true;
            messageListBox.IsEnabled = true;
            fromTextBlock.IsEnabled = true;
            toTextBlock.IsEnabled = true;
            subjectTextBlock.IsEnabled = true;
            bodyTextBox.IsEnabled = true;
            sendButton.IsEnabled = true;
            attachButton.IsEnabled = true;

        }
        private async Task FetchFoldersAsync()
        {
            await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(username, password);
            fromTextBlock.Text = username;
            var folders = client.GetFolders(client.PersonalNamespaces[0]);
            foreach (var folder in folders)
            {
                folderListBox.Items.Add(new ListBoxItem { Content = $"{folder.Name} ({folder.FullName})", Tag = folder });
            }
        }

        private async void FolderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = folderListBox.SelectedItem as ListBoxItem;

            if (selectedItem != null && selectedItem.Tag is IMailFolder selectedFolder)
            {
                await FetchMessagesFromFolderAsync(selectedFolder);
            }
        }

        private async Task FetchMessagesFromFolderAsync(IMailFolder folder)
        {
            using (var client = new ImapClient())
            {
                await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(username, password);

                await folder.OpenAsync(FolderAccess.ReadOnly);

                var messages = await folder.FetchAsync(0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope);

                messageListBox.Items.Clear();

                foreach (var message in messages.Reverse())
                {
                    messageListBox.Items.Add($"{message.UniqueId} :: {message.Envelope.Date} -- {message.Envelope.Subject} {message.Envelope.From} {message.Envelope.To}");
                }
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the email and password fields are empty
            if (string.IsNullOrWhiteSpace(emailTextBox.Text))
            {
                MessageBox.Show("Please enter your email address.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Stop the login process
            }

            if (string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                MessageBox.Show("Please enter your password.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Stop the login process
            }

            // Perform ComboBox validation
            if (!IsEmailProviderMatch())
            {
                MessageBox.Show("Email address doesn't match the selected provider.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Stop the login process
            }
            if(emailTextBox.Text==username&&passwordBox.Password==password)
            {
                EnableInputFields();
                await FetchFoldersAsync();
            }
            else { MessageBox.Show("Incorect login or pass"); }
            
        }

        private bool IsEmailProviderMatch()
        {
            if (mailServiceComboBox.SelectedItem == null)
            {
                return false; // No provider selected
            }

            string selectedProvider = mailServiceComboBox.SelectedItem.ToString();
            string email = emailTextBox.Text.ToLower();

            switch (selectedProvider)
            {
                case "Gmail":
                    return email.EndsWith("@gmail.com");
                case "Outlook":
                    return email.EndsWith("@outlook.com") || email.EndsWith("@hotmail.com");
                case "Yahoo":
                    return email.EndsWith("@yahoo.com");
                default:
                    return false; // Unknown provider
            }
        }


        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string fromAddress = fromTextBlock.Text;
            string toAddress = toTextBlock.Text;
            string subject = subjectTextBlock.Text;
            string body = bodyTextBox.Text;

            MailMessage message = new MailMessage(fromAddress, toAddress, subject, body)
            {
                Priority = MailPriority.High
            };

            foreach (string fileName in attachBox.Items)
            {
                Attachment attachment = new Attachment(fileName);
                message.Attachments.Add(attachment);
            }

            SmtpClient client = new SmtpClient(server, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };

            client.SendCompleted -= Client_SendCompleted;
            client.SendCompleted += Client_SendCompleted;

            await client.SendMailAsync(message);
        }

        private void Client_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Message sending cancelled.");
            }
            else if (e.Error != null)
            {
                MessageBox.Show($"Error occurred while sending message: {e.Error.Message}");
            }
            else
            {
                MessageBox.Show("Message sent successfully.");
            }
        }

        private void attachButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

           

            if (dialog.ShowDialog() == true)
            {
                attachBox.Items.Add(dialog.FileName);
            }
        }

        private async void messageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            var selectedItem = listBox.SelectedItem as string;

            if (selectedItem != null)
            {
                ContextMenu contextMenu = new ContextMenu();

                MenuItem readMenuItem = new MenuItem { Header = "Прочитати" };
                readMenuItem.Click += async (s, ev) =>
                {
                    if (folderListBox.SelectedItem is ListBoxItem selectedFolderItem && selectedFolderItem.Tag is IMailFolder selectedFolder)
                    {
                        var uniqueIdString = selectedItem.Split(" :: ")[0];
                        var uniqueId = UniqueId.Parse(uniqueIdString);

                        using (var client = new ImapClient())
                        {
                            await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
                            await client.AuthenticateAsync(username, password);

                            selectedFolder = client.GetFolder(selectedFolder.FullName);
                            await selectedFolder.OpenAsync(FolderAccess.ReadOnly);

                            var message = await selectedFolder.GetMessageAsync(uniqueId);

                            ReadMessage readWindow = new Practise_.ReadMessage();
                            readWindow.SetMessageContent(message.TextBody);
                            readWindow.Show();
                        }
                    }
                };

                MenuItem replyMenuItem = new MenuItem { Header = "Відповісти" };
                replyMenuItem.Click += async (s, ev) =>
                {
                    if (folderListBox.SelectedItem is ListBoxItem selectedFolderItem && selectedFolderItem.Tag is IMailFolder selectedFolder)
                    {
                        var uniqueIdString = selectedItem.Split(" :: ")[0];
                        var uniqueId = UniqueId.Parse(uniqueIdString);

                        using (var client = new ImapClient())
                        {
                            await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
                            await client.AuthenticateAsync(username, password);

                            selectedFolder = client.GetFolder(selectedFolder.FullName);
                            await selectedFolder.OpenAsync(FolderAccess.ReadOnly);

                            var message = await selectedFolder.GetMessageAsync(uniqueId);

                            string senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address;
                            string subject = "Re: " + message.Subject;

                            toTextBlock.Text = senderEmail;
                            subjectTextBlock.Text = subject;

                            MessageBox.Show("Поле 'To' та 'Subject' заповнені для відповіді.");
                        }
                    }
                };

                // New "Delete" menu item
                MenuItem deleteMenuItem = new MenuItem { Header = "Delete" };
                deleteMenuItem.Click += async (s, ev) =>
                {
                    if (folderListBox.SelectedItem is ListBoxItem selectedFolderItem && selectedFolderItem.Tag is IMailFolder selectedFolder)
                    {
                        var uniqueIdString = selectedItem.Split(" :: ")[0];
                        var uniqueId = UniqueId.Parse(uniqueIdString);

                        using (var client = new ImapClient())
                        {
                            await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
                            await client.AuthenticateAsync(username, password);

                            selectedFolder = client.GetFolder(selectedFolder.FullName);
                            await selectedFolder.OpenAsync(FolderAccess.ReadWrite);

                            await selectedFolder.AddFlagsAsync(new[] { uniqueId }, MessageFlags.Deleted, true);
                            await selectedFolder.ExpungeAsync();

                            MessageBox.Show("Message deleted successfully.");
                        }
                    }
                };

                // New "Move to Archive" menu item
                /*MenuItem archiveMenuItem = new MenuItem { Header = "Move to Archive" };
                archiveMenuItem.Click += async (s, ev) =>
                {
                    if (folderListBox.SelectedItem is ListBoxItem selectedFolderItem && selectedFolderItem.Tag is IMailFolder selectedFolder)
                    {
                        var uniqueIdString = selectedItem.Split(" :: ")[0];
                        var uniqueId = UniqueId.Parse(uniqueIdString);

                        using (var client = new ImapClient())
                        {
                            await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
                            await client.AuthenticateAsync(username, password);

                            selectedFolder = client.GetFolder(selectedFolder.FullName);
                            await selectedFolder.OpenAsync(FolderAccess.ReadWrite);

                            var archiveFolder = client.GetFolder("Archive");
                            await archiveFolder.OpenAsync(FolderAccess.ReadWrite);

                            await selectedFolder.MoveToAsync(new[] { uniqueId }, archiveFolder);

                            MessageBox.Show("Message moved to Archive successfully.");
                        }
                    }
                };*/

                // New "Move to Folder" menu item
                MenuItem moveMenuItem = new MenuItem { Header = "Move to Folder" };
                moveMenuItem.Click += async (s, ev) =>
                {
                    // You can prompt the user to choose a folder for moving the email
                    // For this example, I'm moving it to "Inbox"
                    if (folderListBox.SelectedItem is ListBoxItem selectedFolderItem && selectedFolderItem.Tag is IMailFolder selectedFolder)
                    {
                        var uniqueIdString = selectedItem.Split(" :: ")[0];
                        var uniqueId = UniqueId.Parse(uniqueIdString);

                        using (var client = new ImapClient())
                        {
                            await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
                            await client.AuthenticateAsync(username, password);

                            selectedFolder = client.GetFolder(selectedFolder.FullName);
                            await selectedFolder.OpenAsync(FolderAccess.ReadWrite);

                            var targetFolder = client.Inbox; // Replace with the desired folder

                            await selectedFolder.MoveToAsync(new[] { uniqueId }, targetFolder);

                            MessageBox.Show("Message moved successfully.");
                        }
                    }
                };

                // Add the new menu items to the context menu
                contextMenu.Items.Add(readMenuItem);
                contextMenu.Items.Add(replyMenuItem);
                contextMenu.Items.Add(deleteMenuItem);
                //contextMenu.Items.Add(archiveMenuItem);
                contextMenu.Items.Add(moveMenuItem);

                contextMenu.IsOpen = true;
            }
            else
            {
                ContextMenuService.SetContextMenu(listBox, null);
            }
        }

    }
}
