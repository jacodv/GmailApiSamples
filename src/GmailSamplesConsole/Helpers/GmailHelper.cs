using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using GmailSamplesConsole.Models;

namespace GmailSamplesConsole.Helpers
{
  public static class GmailApiHelper
  {
    private static readonly string[] Scopes = { GmailService.Scope.MailGoogleCom };
    private const string ApplicationName = "UtilityData.Command";

    public static async Task<GmailService> GetService(Stream googleSettings)
    {
      var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
        (await GoogleClientSecrets.FromStreamAsync(googleSettings)).Secrets,
        Scopes,
        "user",
        CancellationToken.None,
        new Google.Apis.Util.Store.FileDataStore(@"c:\temp\googleCreds"));

      var service = new GmailService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = ApplicationName,
      });
      return service;
    }

    public static async Task ProcessMessages(GmailService service, GmailSettings settings, bool unReadOnly = true)
    {
      var emailMessages = new List<GmailMessage>();
      var listRequest = service.Users.Messages.List(settings.EmailAddress);
      listRequest.LabelIds = "INBOX";
      listRequest.IncludeSpamTrash = false;
      if (unReadOnly)
        listRequest.Q = "is:unread"; //ONLY FOR UNDREAD EMAIL'S...

      //GET ALL EMAILS
      var listResponse = await listRequest.ExecuteAsync();

      if (listResponse?.Messages?.Any() == true)
      {
        foreach (var messageSummary in listResponse.Messages)
        {
          try
          {
            var getMessageRequest = service.Users.Messages.Get(settings.EmailAddress, messageSummary.Id);

            //MAKE ANOTHER REQUEST FOR THAT EMAIL ID...
            var messageDetail = await getMessageRequest.ExecuteAsync();
            if (messageDetail == null)
              throw new InvalidOperationException($"Failed to process message: {messageSummary.Id}");

            var message = GmailMessage.FromMessageContent(messageDetail);
            Console.WriteLine($"Processing: {message}");

            //READ MAIL BODY
            Console.WriteLine("STEP-2: Read Mail Body");
            var attachments = await GetAttachments(service, settings.EmailAddress, messageSummary.Id);
            if (!attachments.Any())
            {
              Console.WriteLine($"No attachments for: {message.Subject}|{message.Id}");
            }
            else
            {
              foreach (var attachment in attachments)
              {
                Console.WriteLine($"Attachment:{attachment.Name}({attachment.Data.Length})");
              }
            }

            //MESSAGE MARKS AS READ AFTER READING MESSAGE
            await MarkMessageAsRead(service, settings.EmailAddress, messageSummary.Id);
          }
          catch (Exception e)
          {
            Console.WriteLine(e);
            throw;
          }
        }
      }
    }
    public static async Task<List<GmailAttachment>> GetAttachments(GmailService service, string userId, string messageId)
    {
      try
      {
        var attachments = new List<GmailAttachment>();
        var message = await service.Users.Messages.Get(userId, messageId).ExecuteAsync();
        var parts = message.Payload.Parts;

        foreach (var part in parts)
        {
          if (string.IsNullOrEmpty(part.Filename))
            continue;

          var attId = part.Body.AttachmentId;
          var attachPart = await service.Users.Messages.Attachments.Get(userId, messageId, attId).ExecuteAsync();

          attachments.Add(new GmailAttachment()
          {
            Name = part.Filename,
            Data = Convert.FromBase64String(attachPart.Data.Replace('-', '+').Replace('_', '/'))
          });
        }
        return attachments;
      }
      catch (Exception e)
      {
        Console.WriteLine("An error occurred: " + e.Message);
        return null;
      }
    }
    public static async Task MarkMessageAsRead(GmailService service, string emailAddress, string messageId)
    {
      //MESSAGE MARKS AS READ AFTER READING MESSAGE
      var mods = new ModifyMessageRequest();
      mods.AddLabelIds = null;
      mods.RemoveLabelIds = new List<string> { "UNREAD" };
      await service.Users.Messages.Modify(mods, emailAddress, messageId).ExecuteAsync();
    }
    public static bool IsBase64String(string base64, out Span<byte> buffer)
    {
      buffer = new Span<byte>(new byte[base64.Length]);
      return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
    }
  }
}
