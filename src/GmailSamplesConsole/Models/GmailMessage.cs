using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using GmailSamplesConsole.Helpers;
using Google.Apis.Gmail.v1.Data;

  namespace GmailSamplesConsole.Models
  {
    public class GmailMessage
    {
      public GmailMessage()
      {
        Attachments = new List<GmailAttachment>();
      }

      public string Id { get; set; }
      public MailAddress From { get; set; }
      public MailAddress To { get; set; }
      public string Body { get; set; }
      public string Subject { get; set; }
      public DateTime MailDateTime { get; set; }
      public List<GmailAttachment> Attachments { get; set; }

      #region Overrides of Object

      public override string ToString()
      {
        return $"{Subject}|{Id}|{From}|{MailDateTime}";
      }

      #endregion

      public static GmailMessage FromMessageContent(Message message)
      {
        var result = new GmailMessage();
        result.Id = message.Id;

        result.Body = message.Payload.Parts == null && message.Payload.Body != null
          ? message.Payload.Body.Data
          : GetMessageBody(message.Payload.Parts);

        if (message.InternalDate != null)
          result.MailDateTime = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).DateTime;

        foreach (var parts in message.Payload.Headers)
        {
          switch (parts.Name)
          {
            case "To":
              result.To = new MailAddress(parts.Value);
              break;
            case "From":
              result.From = new MailAddress(parts.Value);
              break;
            case "Subject":
              result.Subject = parts.Value;
              break;
          }
        }

        return result;
      }

      public static string GetMessageBody(IList<MessagePart> parts)
      {
        if (!parts.Any())
          return string.Empty;

        var mailParts = parts
          .Where(x => (x.MimeType is "text/plain" or "multipart/alternative"))
          .ToList();

        if (!mailParts.Any())
          return string.Empty;

        var messageBody = new StringBuilder();
        foreach (var part in mailParts)
        {
          if (part.Parts == null)
          {
            if (part.Body?.Data == null)
              continue;

            var cleanedString = part.Body.Data.Replace('-', '+').Replace('_', '/');
            var isBase64 = GmailApiHelper.IsBase64String(cleanedString, out var buffer);
            messageBody.AppendLine(isBase64 ? Encoding.UTF8.GetString(buffer) : part.Body.Data);
          }
          else
            return GetMessageBody(part.Parts);
        }

        return messageBody.ToString();
      }
    }

    public class GmailAttachment
    {
      public string Name { get; set; }
      public byte[] Data { get; set; }
    }
  }