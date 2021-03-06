﻿using _4us2watch.Models;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Firebase.Database.Query;

namespace _4us2watch.Components
{
    public class ReaderWriter
    {
        private static readonly string ChildName = "Persons";
        static FirebaseClient firebase = new FirebaseClient("https://us2watch-a3e6d-default-rtdb.europe-west1.firebasedatabase.app/");
        public static async Task<List<User>> GetAllPersons()
        {
            return (await firebase
            .Child(ChildName)
            .OnceAsync<User>()).Select(item => new User
            {
                username = item.Object.username,
                email = item.Object.email,
                friends = item.Object.friends,
                movies = item.Object.movies
            }).ToList();
        }
        public static async Task AddPerson(string _username, string _email, List<string> _friends, List<string> _movies)
        {
            //var friendsSer = JsonConvert.SerializeObject(_friends);
            //var moviesSer = JsonConvert.SerializeObject(_movies);
            var uporabnik = new User() { username = _username, email = _email, friends = _friends, movies = _movies };
            var uporabnikSer = JsonConvert.SerializeObject(uporabnik);

            await firebase
            .Child(ChildName)
            .PostAsync(uporabnikSer);
        }
        public static async Task<User> GetPerson(string email)
        {
            var allPersons = await GetAllPersons();
            await firebase
            .Child(ChildName)
            .OnceAsync<User>();
            return allPersons.FirstOrDefault(a => a.email == email);
        }

        public static async Task<User> GetPersonByUsername(string Username)
        {
            var allPersons = await GetAllPersons();
            await firebase
            .Child(ChildName)
            .OnceAsync<User>();
            return allPersons.FirstOrDefault(x => x.username == Username);
        }

        public static async Task UpdatePerson(string _username, string _email, List<string> _friends, List<string> _movies)
        {
            var toUpdatePerson = (await firebase
            .Child(ChildName)
            .OnceAsync<User>()).FirstOrDefault(a => a.Object.email == _email);
            await firebase
            .Child(ChildName)
            .Child(toUpdatePerson.Key)
            .PutAsync(new User() { username = _username, email = _email, friends = _friends, movies = _movies });
        }

        public static async Task UpdateFriendsList(string _email, User user)
        {
            var toUpdatePerson = (await firebase
            .Child(ChildName)
            .OnceAsync<User>()).FirstOrDefault(x => x.Object.email == _email);

            toUpdatePerson.Object.friends.Add(user.username);

            await firebase
            .Child(ChildName)
            .Child(toUpdatePerson.Key)
            .PutAsync(toUpdatePerson.Object);
        }

        public static async Task<List<string>> GetUserMovies(string _email)
        {
            var userMovie = (await firebase
            .Child(ChildName)
            .OnceAsync<User>()).FirstOrDefault(x => x.Object.email == _email);

            return userMovie.Object.movies;
        }

        public static async Task<List<string>> GetUserMoviesByUsername(string username)
        {
            var userMovie = (await firebase
            .Child(ChildName)
            .OnceAsync<User>()).FirstOrDefault(x => x.Object.username == username);

            return userMovie.Object.movies;
        }

        public async Task DeletePerson(string email)
        {
            var toDeletePerson = (await firebase
            .Child(ChildName)
            .OnceAsync<User>()).FirstOrDefault(a => a.Object.email == email);
            await firebase.Child(ChildName).Child(toDeletePerson.Key).DeleteAsync();
        }

    }
}
