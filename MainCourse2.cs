﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Resturant_data.Bot.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UserDeatalisModel.Bot.Models;

namespace ContosoScuba.Bot.CardProviders
{
    public class MainCourse2 : CardProvider
    {
        //public override string CardName => "2-Locations";
        public override string CardName => "main_courses";


        public override bool ProvidesCard(UserDetalisData ResturantData, JObject value, string messageText, UserDetalisData ResturantData1)
        {
            string text = string.Empty;
            if (value != null && ((Newtonsoft.Json.Linq.JProperty)value.First).Name == "Dish")
                text = value.GetValue("Dish").ToString();
            //var text = JObject.Parse(value)["main course"];
            return (ResturantData != null && text == "Main course");
        }

        public override async Task<UserCardResult> GetCardResult(UserDetalisData ResturantData, JObject value, string messageText)
        {
            var Dish = value != null ? value.Value<string>("Dish") : messageText;

            var error = GetErrorMessage(Dish);
            if (!string.IsNullOrEmpty(error))
                return new UserCardResult() { ErrorMessage = error };

            ResturantData.Dish = Dish;


            return new UserCardResult() { CardText = await GetCardText(ResturantData) };
        }

        private async Task<string> GetCardText(UserDetalisData ResturantData)
        {
            var replaceInfo = new Dictionary<string, string>();
            replaceInfo.Add("{{Dish}}", ResturantData.Dish);

            return await base.GetCardText(replaceInfo);
        }

        private string GetErrorMessage(string userInput)
        {
            if (IsSchool(userInput))
            {
                return string.Empty;
            }
            return "Please enter Veg stater, Main course, Dessert, or Soups. You can also click the card button to make your selection.";
        }

        private bool IsSchool(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
                return false;

            var lowered = userInput.ToLower();
            return (lowered.Contains("main course"));
        }

    }
}