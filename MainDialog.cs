using Microsoft.Bot.Builder.Dialogs;
using RestaurantBot;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using RestaurantBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using ContosoScuba.Bot.Dialogs;
using ContosoScuba.Bot.Services;
using Newtonsoft.Json;
using SpeechToText;
using Bot_Application_mail;
using ContosoScuba.Bot.CardProviders;
using System.Text;
using UserDeatalisModel.Bot.Models;

namespace RestaurantBot
{

    // I just left these in so you can plug and play
    [LuisModel("e2fb5dc3-8e97-4d8c-a068-54d38c6545e4", "f017f470eff645c898f139b24dc3948c")]
    [Serializable]
    public class MainDialog : LuisDialog<Object>, IDialog<object>
    {
        public string Email;
        public DateTime date;
        [LuisIntent("None")]
        //[LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            try
            {
                var userMsg = result.Query;
                // Do some basic keyword checking
                if (Regex.IsMatch(userMsg, @"\b(hello|hi|hey)\b", RegexOptions.IgnoreCase))
                {
                    await context.PostAsync("Hey there! I can help you make bookings and ask me other stuff's.You can use the intents like View menu,Make booking,Cancel Booking,View Booking");

                }
                else if (Regex.IsMatch(userMsg, @"\b(thank|thanks)\b", RegexOptions.IgnoreCase))
                {
                    await context.PostAsync("You're welcome.");
                    await context.PostAsync("Thank you visit again.");
                }
                else if (Regex.IsMatch(userMsg, @"\b(bye|goodbye)\b", RegexOptions.IgnoreCase))
                {
                    await context.PostAsync("Okay, bye for now.");
                }
                else if (Regex.IsMatch(userMsg, @"\b(Happy)\b", RegexOptions.IgnoreCase))
                {
                    await context.PostAsync("I'm happy you're happy");
                }

                else
                {
                    await context.PostAsync("Hmm I'm not sure what you want. Still learning, sorry!");
                }
            }
            catch (Exception)
            {
                await context.PostAsync("Argh something went wrong :( Sorry about that.");
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("ViewMenu")]
        public async Task ViewMenu(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            try
            {
                await context.PostAsync("sure thing - I'll need some details");
                await context.PostAsync("select your menu by typing menu");

                //RootDialog res = new RootDialog();
                //await res.StartAsync(context);
                context.Wait(MessageReceivedAsync);
                //await MessageReceivedAsync(context, activity, result);
            }
            catch (Exception)
            {
                await context.PostAsync("Something really bad happened. You can try again later meanwhile I'll check what went wrong.");
                context.Wait(MessageReceived);
            }
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            LuisResult luisResult = new LuisResult();
            var activity = await result as Activity;
            string text = string.IsNullOrEmpty(activity.Text) ? string.Empty : activity.Text.ToLower();

            IMessageActivity nextMessage = null;

            if (!string.IsNullOrEmpty(text) && text.ToLower() == "cancel booking")
                await CancelBooking(context, luisResult);
            if (!string.IsNullOrEmpty(text) && text.ToLower() == "make booking")
                await MakeBooking(context, luisResult);
            if (!string.IsNullOrEmpty(text) && text.ToLower() == "view booking")
                await ViewBooking(context, luisResult);
            if (!string.IsNullOrEmpty(text) && text.ToLower() == "operating hours")
                await ViewBooking(context, luisResult);
            if (!string.IsNullOrEmpty(text) && text.ToLower() == "get location")
                await ViewBooking(context, luisResult);


            else
            {
                nextMessage = await GetMessageFromText(context, activity, text);
            }

            if (nextMessage == null)
                nextMessage = await GetNextScubaMessage(context, activity);

            await context.PostAsync(nextMessage);

            //if (activity.Value != null && ((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)activity.Value).First).Name == "date")
            //{
            //    MainDialog main = new MainDialog();
            //    await main.Display(context);
            //}
            //await MessageReceivedAsync(context, result, luisResult);
            //context.Wait(MessageReceivedAsync);
        }


        private async Task<IMessageActivity> GetMessageFromText(IDialogContext context, Activity activity, string text)
        {
            IMessageActivity nextMessage = null;

            /*if (text.Contains("wildlife"))
            {
                return nextMessage = await GetCard(activity, "Wildlife");
            }*/
            if (text == "Thank you"
                    || text == "Thanks"
                    || text == "Thankyou"
                    || text == "thanks"
                    || text == "thank you")
            {
                return nextMessage = await GetCard(activity, "Thank_you");
            }
            /* else if (text.Contains("danger"))
             {
                 return nextMessage = await GetCard(activity, "Danger");
             }*/
            else if (text == "hi"
                     || text == "hello"
                     || text == "reset"
                     || text == "start over"
                     || text == "restart")
            {
                //clear conversation data, since the user has decided to restart
                context.PrivateConversationData.Clear();
                nextMessage = await GetCard(activity, "0-Welcome");
            }
            else if (text == "make booking" || text == "operating hours")
            {
                return nextMessage = await GetCard(activity, "Thank_you");
            }
            return nextMessage;
        }

        private async Task<IMessageActivity> GetCard(Activity activity, string cardName)
        {
            var cardText = await ScubaCardService.GetCardText(cardName);
            return GetCardReply(activity, cardText);
        }

        private async Task<IMessageActivity> GetNextScubaMessage(IDialogContext context, Activity activity)
        {
            var resultInfo = await new ScubaCardService().GetNextCardText(context, activity);
            if (!string.IsNullOrEmpty(resultInfo.ErrorMessage))
                return activity.CreateReply(resultInfo.ErrorMessage);

            return GetCardReply(activity, resultInfo.CardText);
        }

        public static Activity GetCardReply(Activity activity, string cardText)
        {
            var reply = JsonConvert.DeserializeObject<Activity>(cardText);
            if (reply.Attachments == null)
                reply.Attachments = new List<Attachment>();

            var tempReply = activity.CreateReply("");
            reply.ChannelId = tempReply.ChannelId;
            reply.Timestamp = tempReply.Timestamp;
            reply.From = tempReply.From;
            reply.Conversation = tempReply.Conversation;
            reply.Recipient = tempReply.Recipient;
            reply.Id = tempReply.Id;
            reply.ReplyToId = tempReply.ReplyToId;
            if (reply.Type == null)
                reply.Type = ActivityTypes.Message;

            return reply;
        }

        [LuisIntent("MakeBooking")]
        public async Task MakeBooking(IDialogContext context, LuisResult result)
        {
            try
            {
                var entities = new List<EntityRecommendation>(result.Entities);
                //Chronic.Parser parser = new Chronic.Parser();
                //EntityRecommendation date = new EntityRecommendation();
                //result.TryFindEntity("builtin.datetime.date", out date);
                //var dateResult = parser.Parse(date.Entity);
                EntityRecommendation entityDate;
                EntityRecommendation entityTime;

                result.TryFindEntity("builtin.datetime.date", out entityDate);
                result.TryFindEntity("builtin.datetime.time", out entityTime);

                if ((entityDate != null) & (entityTime != null))
                {
                    entities.Add(new EntityRecommendation(type: "Date") { Entity = entityDate.Entity });
                    entities.Add(new EntityRecommendation(type: "Time") { Entity = entityTime.Entity });
                }
                else if (entityDate != null)
                {
                    entities.Add(new EntityRecommendation(type: "Date") { Entity = entityDate.Entity });
                }
                else if (entityTime != null)
                {
                    // I use resolution instead of entity for time, because things like 9.30pm don't work with entity (it's an issue with LUIS atm)
                    entities.Add(new EntityRecommendation(type: "Time") { Entity = entityTime.Entity });
                }
                await context.PostAsync("Sure thing - I'll need some details from you.");
                var bookingForm = new FormDialog<BookingForm>(new BookingForm(), BookingForm.BuildForm, FormOptions.PromptInStart, entities);
                context.Call(bookingForm, BookingFormComplete);
            }
            catch (Exception)
            {
                await context.PostAsync("Something really bad happened. You can try again later meanwhile I'll check what went wrong.");
                context.Wait(MessageReceived);
            }
        }
        private async Task BookingFormComplete(IDialogContext context, IAwaitable<BookingForm> result)
        {

            try
            {
                BookingForm bookingform = await result;
                SaveBookingAsync(context, bookingform);
                //sending the email
                await Email_send.Execute(bookingform, "confirmed booking");

                await context.PostAsync("Your booking is confirmed.");

                Number number = new Number();
                number.sendSms(bookingform.PhNum, bookingform.Name);


            }
            catch (FormCanceledException)
            {
                await context.PostAsync("I did not make your booking.");
            }
            catch (Exception)
            {
                await context.PostAsync("Something really bad happened. You can try again later meanwhile I'll check what went wrong.");
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }
        private async Task SaveBookingAsync(IDialogContext context, BookingForm bookingform)
        {
            var booking = new Booking();
            SaveToDB(bookingform);

            if (bookingform.Time != null)
            {
                // Time stated separately
                var time = bookingform.Time/*.GetValueOrDefault()*/;
                booking.BookingDateTime = bookingform.Date.Date.Add(time.TimeOfDay);

            }
            else
            {
                booking.BookingDateTime = bookingform.Date;
            }
            booking.Name = bookingform.Name;
            booking.NumPeople = bookingform.NumPeople;
            booking.PhNum = bookingform.PhNum;
            booking.Requests = bookingform.Requests;
            context.UserData.SetValue<Booking>("booking", booking);
        }

        private void SaveToDB(BookingForm bookingform)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection())
                {
                    var time = bookingform.Time;
                    DateTime UserDateTime = bookingform.Date.Date.Add(time.TimeOfDay);
                    // Create the connectionString
                    // Trusted_Connection is used to denote the connection uses Windows Authentication
                    conn.ConnectionString = "Data Source=DESKTOP-DM87KPQ\\SQLEXPRESS;Initial Catalog=Restaurant;Integrated Security=True";
                    conn.Open();
                    Console.WriteLine(bookingform.Requests);
                    SqlCommand insertCommand = new SqlCommand("INSERT INTO Final_UserDetalis (Name, Date, Time, NumPeople, PhNum, Requests, Email_ID) VALUES (@0, @1, @2, @3, @4, @5, @6)", conn);

                    insertCommand.Parameters.Add(new SqlParameter("0", bookingform.Name));
                    insertCommand.Parameters.Add(new SqlParameter("1", UserDateTime));
                    insertCommand.Parameters.Add(new SqlParameter("2", bookingform.Time));
                    insertCommand.Parameters.Add(new SqlParameter("3", bookingform.NumPeople));
                    insertCommand.Parameters.Add(new SqlParameter("4", bookingform.PhNum));
                    insertCommand.Parameters.Add(new SqlParameter("5", bookingform.Requests));
                    insertCommand.Parameters.Add(new SqlParameter("6", bookingform.Email_ID));


                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {

                Console.WriteLine(ex);
            }
        }

        [LuisIntent("CancelBooking")]
        public async Task CancelBooking(IDialogContext context, LuisResult result)
        {
            Booking booking;


            PromptDialog.Text(context, TakeDate, "Enter the  Email_Id");
            /* PromptDialog.Confirm(
                    context,
                    AfterCancelBooking,
                    "Are you sure you want to cancel your current booking for " + booking.BookingDateTime + "? (Y/N)",
                    "Cancel current booking? (Y/N)",
                    promptStyle: PromptStyle.Auto);*/

        }
        public async Task AfterCancelBooking(IDialogContext context, IAwaitable<bool> argument)
        {
            //Booking booking;
            //context.UserData.TryGetValue<Booking>("booking", out booking);
            //var Entered_date = await context;
            // DateTime date = new DateTime();
            BookingForm bookingform = new BookingForm();
            try
            {
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = "Data Source=DESKTOP-DM87KPQ\\SQLEXPRESS;Initial Catalog=Restaurant;Integrated Security=True";
                    conn.Open();
                    SqlCommand deleteCommand = new SqlCommand("DELETE FROM Final_UserDetalis WHERE Email_ID=@Email_ID and Date =@Date", conn);

                    deleteCommand.Parameters.Add(new SqlParameter("@Email_ID", Email));
                    deleteCommand.Parameters.Add(new SqlParameter("@Date", date));
                    deleteCommand.ExecuteNonQuery();
                    //SqlDataReader reader2 = deleteCommand.ExecuteNonQuery();

                    await context.PostAsync("Your booking has been cancelled.");
                    bookingform.Email_ID = Email;
                    bookingform.Name = bookingform.Name;
                    await Email_send.Execute(bookingform, "cancel booking");

                    context.UserData.RemoveValue("booking");
                    conn.Close();

                }
            }
            catch (Exception ex)
            {

                await context.PostAsync("You have no current bookings.");
                context.Wait(MessageReceived);
            }

        }

        private async Task TakeDate(IDialogContext context, IAwaitable<string> result)
        {
            //throw new NotImplementedException();
            Email = await result;
            try
            {
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = "Data Source=DESKTOP-DM87KPQ\\SQLEXPRESS;Initial Catalog=Restaurant;Integrated Security=True";
                    conn.Open();
                    bool valid, valid1;

                    // SqlCommand cmdSql = new SqlCommand("ResturantProcedure", conn);
                    // cmdSql.CommandType = CommandType.StoredProcedure;

                    // cmdSql.ExecuteNonQuery();
                    //to check for valid email_id
                    SqlCommand command = new SqlCommand("select Date FROM Final_UserDetalis where Email_ID=@0", conn);
                    //await context.PostAsync(Date);
                    command.Parameters.Add(new SqlParameter("0", Email));
                    SqlDataReader sdr = command.ExecuteReader();
                    try
                    {
                        if (sdr.HasRows)
                        {
                            while (sdr.Read())
                            {
                                DateTime dt = Convert.ToDateTime(sdr[0]);
                                await context.PostAsync("You have booking at" + dt.ToString());
                            }
                            PromptDialog.Text(context, deleteFromDBAsync, "Enter the Date to cancel the order in format YYYY/MM/dd hh:mm:ss");
                        }
                        else
                        {
                            await context.PostAsync("You have no current bookings.");
                        }
                    }
                    catch (Exception e1)
                    {
                        await context.PostAsync("You have no current bookings.");
                    }

                }
            }
            catch (Exception e)
            {

            }


        }

        public async Task Display(IDialogContext context)
        {
            //IDialogContext dialogContext = null;

            await context.PostAsync("Enter View Menu to proceed.");
        }

        private async Task deleteFromDBAsync(IDialogContext context, IAwaitable<string> result)
        {
            //throw new NotImplementedException();
            // var Email = await result;
            var Entered_date = await result;
            date = Convert.ToDateTime(Entered_date);
            // date = Convert.ToDateTime(DateTime.ParseExact(Entered_date, "dd-MM-yyyy", CultureInfo.InvariantCulture));
            //string datetime = date.ToString("dd-MM-yyyy");
            PromptDialog.Confirm(
                       context,
                       AfterCancelBooking,
                       "Are you sure you want to cancel your current booking for " + Entered_date + "? (Y/N)",
                       "Cancel current booking? (Y/N)",
                       promptStyle: PromptStyle.Auto);


        }

        [LuisIntent("ViewBooking")]
        public async Task ViewBooking(IDialogContext context, LuisResult result)
        {
            Booking booking;

            //   await context.PostAsync(booking.Name + " have a booking at " + booking.BookingDateTime + ".");
            PromptDialog.Text(context, selectFromDBAsync, "Enter the  Email_Id to view booking");


            /*     finally
                 {
                     context.Wait(MessageReceived);
                 }*/
        }
        private async Task selectFromDBAsync(IDialogContext context, IAwaitable<string> result)
        {
            Booking booking;
            string response = await result;
            string email = response;
            Console.WriteLine(email);
            MainDialog mainobj = new MainDialog();
            Boolean result1 = mainobj.ValidateEmailID(email);
            if (result1 == true)
            {


                try
                {
                    using (SqlConnection conn = new SqlConnection())
                    {
                        // Create the connectionString
                        // Trusted_Connection is used to denote the connection uses Windows Authentication
                        conn.ConnectionString = "Data Source=DESKTOP-DM87KPQ\\SQLEXPRESS;Initial Catalog=Restaurant;Integrated Security=True";
                        // SqlCommand cmdSql = new SqlCommand("ResturantProcedure", conn);
                        //cmdSql.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        //  cmdSql.ExecuteNonQuery();

                        SqlCommand cmdSql2 = new SqlCommand("select Name,Time from Final_UserDetalis where Email_ID=@Email_ID", conn);
                        cmdSql2.Parameters.Add(new SqlParameter("@Email_ID", email));
                        SqlDataReader reader = cmdSql2.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string name = reader.GetString(0);
                                DateTime dateTime = DateTime.Parse(reader.GetString(1));
                                await context.PostAsync(name + " have a booking at " + dateTime + ".");
                            }
                        }
                        else
                        {
                            await context.PostAsync("You have no booking!");
                        }
                        conn.Close();
                    }
                }
                catch (Exception e)
                {
                    await context.PostAsync("Renter the proper Emailid");
                }
            }
            else
            {
                PromptDialog.Text(context: context,
                    prompt: "please ReEnter the proper Email_Id",
                    resume: selectFromDBAsync,
                    retry: "Sorry.Renter");
            }
        }
        public Boolean ValidateEmailID(string email)
        {
            var result = false;//= new ValidateResult();
            var emailid = email.ToString();
            if (!Regex.IsMatch(emailid, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase))
            {
                // If time not available
                result = false;
                // result.Feedback = "Enter the valid Email";
            }
            else
            {
                result = true;
                // result.Value = response;
            }
            return (result);
        }

        [LuisIntent("OpeningHours")]
        public async Task OpeningHours(IDialogContext context, LuisResult result)
        {
            try
            {
                await context.PostAsync("Here are our opening hours: ");
                await context.PostAsync("Monday to Friday: 8.00am to 17.00pm(5.pm) \n\n" +
        "Saturday and Sunday: 8.00am to 13.00pm(1.pm) \n\n");
            }
            catch (Exception)
            {
                await context.PostAsync("Something went wrong, sorry :(");
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }
        [LuisIntent("GetLocation")]
        public async Task GetLocation(IDialogContext context, LuisResult result)
        {
            try
            {
                await context.PostAsync("click here to this link for image");
                var map_location = "https://www.google.com/maps/place/Zaica+Dine+%26+Wine/@12.971411,77.6436155,12z/data=!4m18!1m12!4m11!1m3!2m2!1d77.7139967!2d12.9750912!1m6!1m2!1s0x3bae12235d6a0adb:0xd9344722c11813a!2szaica+dine+and+wine+kundalahalli!2m2!1d77.714686!2d12.9674056!3m4!1s0x3bae12235d6a0adb:0xd9344722c11813a!8m2!3d12.9674056!4d77.714686";
                await context.PostAsync(map_location);
                var reply = context.MakeMessage();
                reply.Attachments = new List<Attachment>()
                    {
                        new Attachment()
                        {
                            ContentUrl = "https://www.google.com/maps/place/Zaica+Dine+%26+Wine/@12.971411,77.6436155,12z/data=!4m18!1m12!4m11!1m3!2m2!1d77.7139967!2d12.9750912!1m6!1m2!1s0x3bae12235d6a0adb:0xd9344722c11813a!2szaica+dine+and+wine+kundalahalli!2m2!1d77.714686!2d12.9674056!3m4!1s0x3bae12235d6a0adb:0xd9344722c11813a!8m2!3d12.9674056!4d77.714686",
                           // ContentType = "image/jpg",
                            Name = "Map.jpg"
                        }
                    };
                await context.PostAsync(reply);
            }
            catch (Exception)
            {
                await context.PostAsync("Something went wrong, sorry :(");
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }
    }
}