using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco_onatrix.Models;
using System.Text.Json;
using System.Threading.Tasks;

namespace Umbraco_onatrix.Controllers
{
	public class ContactSurfaceController : SurfaceController
	{
		private readonly string _serviceBusConnectionString;
		private readonly string _queueName;

		public ContactSurfaceController(
			IUmbracoContextAccessor umbracoContextAccessor,
			IUmbracoDatabaseFactory databaseFactory,
			ServiceContext services,
			AppCaches appCaches,
			IProfilingLogger profilingLogger,
			IPublishedUrlProvider publishedUrlProvider,
			IConfiguration configuration) 
			: base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
		{
			
			_serviceBusConnectionString = configuration.GetConnectionString("ServiceBusConnection");
			_queueName = configuration.GetConnectionString("QueueName");
		}

		[HttpPost]
		public async Task<IActionResult> HandleSubmit(ContactFormModel form)
		{
			if (!ModelState.IsValid)
			{
				ViewData["name"] = form.Name;
				ViewData["email"] = form.Email;
				ViewData["phone"] = form.Phone;

				ViewData["error_name"] = string.IsNullOrEmpty(form.Name);
				ViewData["error_email"] = string.IsNullOrEmpty(form.Email);
				ViewData["error_phone"] = string.IsNullOrEmpty(form.Phone);

				return CurrentUmbracoPage();
			}

			var formData = new
			{
				to = $"{form.Email}",
				subject = $"Hello {form.Name} – Thank You for Reaching Out!",
				HtmlBody = $@"
				<html>
				<body style='font-family:Arial, sans-serif; line-height:1.6; background-color:#ffffff; margin:0; padding:0;'>
					<div style='background-color:#4F5955; padding:20px; border-radius:10px 10px 0 0;'>
						<h2 style='color:#D9C3A9; margin:0;'>Hello {form.Name},</h2>
					</div>

					<div style='background-color:#D9C3A9; padding:20px;'>
						<p style='color:#555;'>We are excited to hear from you! Thank you for getting in touch with us.</p>
						<p style='color:#555;'>
							Our team has received your message and will get back to you as soon as possible. If you have any urgent questions, feel free to reply directly to this email.
						</p>
						<p style='color:#555;'>
							<strong>Your Contact Information:</strong><br />
							Email: {form.Email}<br />
							Phone: {form.Phone}
						</p>
						<p style='color:#555;'>We look forward to assisting you.</p>
					</div>

					<div style='background-color:#4F5955; padding:20px; border-radius:0 0 10px 10px;'>
						<p style='color:#D9C3A9; margin:0;'>
							Best regards,<br />
							<strong>The Team at [Your Company]</strong>
						</p>
					</div>
				</body>
				</html>",

				PlainText = $"Hello {form.Name},\n\nThank you for contacting us! We have received your message and will get back to you soon.\n\nYour contact information:\nEmail: {form.Email}\nPhone: {form.Phone}\n\nBest regards,\nThe Team at [Your Company]"
			};



			try
			{
				
				await using var client = new ServiceBusClient(_serviceBusConnectionString);
				ServiceBusSender sender = client.CreateSender(_queueName);

				
				var messageBody = JsonSerializer.Serialize(formData);

				
				var message = new ServiceBusMessage(messageBody);

				
				await sender.SendMessageAsync(message);

				ViewData["success"] = "Thanks for contacting us";
			}
			
			catch (Exception ex)
			{
				ViewData["error"] = $"An unexpected error occurred: {ex.Message}";
			}

			return CurrentUmbracoPage();
		}
	}
}
