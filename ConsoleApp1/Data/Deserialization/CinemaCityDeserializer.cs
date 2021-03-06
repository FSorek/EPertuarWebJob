﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using ConsoleApp1.Tools;
using EpertuarWebJob;
using EpertuarWebJob.Models;
using EPertuarWebJob.Data.Access;

namespace EPertuarWebJob.Data.Deserialization
{
    public class CinemaCityDeserializer
    {
        private CinemaCity root;

        public List<MovieItem> Deserialize(string json, int cinemaId)
        {
            root = CinemaCity.FromJson(json);

            return MapMovie(root, cinemaId);
        }

        public List<MovieItem> MapMovie(CinemaCity from, int cinemaId)
        {
            List<MovieItem> mappedList = new List<MovieItem>();

            foreach (CityFilm film in from.Body.Films)
            {
                List<string> genres = new List<string>();
                foreach(var genre in film.AttributeIds)
                {
                    genres.Add(genre.ToLower());
                }
                mappedList.Add(new MovieItem
                {
                    Id_Movie = film.Id,
                    Name = film.Name,
                    Director = null,
                    Storyline = null,
                    Trailer = film.VideoLink,
                    Length = (int)film.Length,
                    Original_Name = null,
                    Writers = null,
                    Stars = null,
                    Music = null,
                    Cinematography = null,
                    Rating = null,
                    Shows = MapShow(from, film.Id, cinemaId),
                    Genre = genres
                });

            }
            return mappedList;
        }

        private static List<ShowItem> MapShow(CinemaCity from, string id, int cinemaId)
        {
            List<ShowItem> mappedList = new List<ShowItem>();

            String sql = String.Format("select Id_Cinema from Cinema Where Id_Self = {0} AND CinemaType = {1}", cinemaId, (int)CinemaType.cinemacity);
            SqlConnection con = new SqlConnection(Program.builder.ConnectionString);
            con.Open();
            var IdCinema = 0;
            using (SqlCommand command = new SqlCommand(sql, con))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        IdCinema = reader.GetInt32(0);
                    }
                }
            }
                        

            try
            {
                foreach (Event show in from.Body.Events)
                {
                    if (show.FilmId != id) continue;
                    mappedList.Add(new ShowItem
                    {
                        Id_Movie = 0,
                        Id_Cinema = IdCinema,
                        ShowDate = show.BusinessDay,
                        Start = show.EventDateTime.ToShortTimeString(),
                        is3D = (show.AttributeIds.Contains("2d")),
                        Language = LanguageFinder(show.AttributeIds),

                        Room = -1
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "|" + e.StackTrace);
            }

            return mappedList;
        }

        private static string LanguageFinder(string[] toSearch)
        {
            if (toSearch.Contains("dubbed") || toSearch.Contains("original-lang-pl"))
                return "PL";
            else return "EN";
        }
    }
}
