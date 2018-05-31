using ConsoleApp1.Tools;
using EpertuarWebJob.Models;
using EPertuarWebJob.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EpertuarWebJob
{
    class Program
    {
        public static SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
        {
            DataSource = "epertuar-server.database.windows.net",
            UserID = "okito",
            Password = "Dragon12",
            InitialCatalog = "EPertuarDB",
            PersistSecurityInfo = false,
            MultipleActiveResultSets = true,
            Encrypt = true,
            TrustServerCertificate = false,
            ConnectTimeout = 30
        };

        static void Main(string[] args)
        {
            try
            {
                DataRequestService request = new DataRequestService();
                UpdateDatabase(request, CinemaType.cinemacity);
                //UpdateDatabase(request, CinemaType.multikino);
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.StackTrace + "|" + e.Message + "|" + e.LineNumber + "|||");
            }
        }

        private static void UpdateDatabase(DataRequestService request, CinemaType cinemaType)
        {
            using (SqlConnection con = new SqlConnection(builder.ConnectionString))
            {
                Console.WriteLine("UpdateDB");
                con.Open();

                string sql = "TRUNCATE TABLE Show";
                using (SqlCommand sqlCommand = new SqlCommand(sql, con))
                    sqlCommand.ExecuteNonQuery();

                sql = "select * from Cinema Where CinemaType=" + ((int)cinemaType).ToString();

                AddMovies(sql, con, request, cinemaType);
                con.Close();
            }
        }

        private static void AddMovies(string sql, SqlConnection con, DataRequestService request, CinemaType cinemaType)
        {
            using (SqlCommand command = new SqlCommand(sql, con))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine("Adding Movies.." + reader.GetString(2));
                        request.ProvideData(cinemaType, reader.GetInt32(1));
                        //ADD MOVIES TO DB
                        foreach (var movie in request.MovieList)
                        {
                            if (movie.Original_Name == null) movie.Original_Name = movie.Name;
                            movie.Name = Regex.Replace(movie.Name, "[^a-zA-Z]", "").ToLower();
                            //Check if movie exists in DB
                            UpdateOrInsertMovies(con, movie);
                        }
                    }
                }
            }
        }

        private static void UpdateOrInsertMovies(SqlConnection con, MovieItem movie)
        {
            string sql = "Select * From Movie Where Name='" + movie.Name + "'";
            using (SqlCommand movieCommand = new SqlCommand(sql, con))
            {
                using (SqlDataReader movieReader = movieCommand.ExecuteReader())
                {
                    if (movieReader.HasRows)
                    {
                        UpdateMovie(con, movie);
                    }
                    else
                    {
                        AddNewMovie(con, movie);
                    }
                }
            }
        }

        private static void UpdateMovie(SqlConnection con, MovieItem movie)
        {
            try
            {
                string sql = String.Format(@"UPDATE MOVIE SET 
                                                        Name=           (COALESCE(Name,'{0}')),
                                                        Original_Name=  (COALESCE(Original_Name,'{1}')), 
                                                        Length=         (COALESCE(Length,{2})), 
                                                        Director=       (COALESCE(Director,'{3}')),
                                                        Writers=        (COALESCE(Writers,'{4}')), 
                                                        Stars=          (COALESCE(Stars,'{5}')), 
                                                        Storyline=      (COALESCE(Storyline,'{6}')), 
                                                        Trailer=        (COALESCE(Trailer,'{7}')), 
                                                        Music=          (COALESCE(Music,'{8}')), 
                                                        Cinematography= (COALESCE(Cinematography,'{9}')), 
                                                        Rating=         (COALESCE(Rating,'{10}')), 
                                                        Id_Self=        (COALESCE(Id_Self,'{11}'))
                                                        OUTPUT INSERTED.Id_Movie
                                                        WHERE           Name='{12}'",
movie.Name, movie.Original_Name, movie.Length, movie.Director, movie.Writers, movie.Stars,
movie.Storyline, movie.Trailer, movie.Music, movie.Cinematography, movie.Rating, movie.Id_Movie, movie.Name);

                SqlCommand sqlCommand = new SqlCommand(sql, con);
                sqlCommand.ExecuteNonQuery();

                AddShowsToMovie(con, movie);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + " | at UpdateMovie, skipping movie");
            }
            
        }

        private static void AddNewMovie(SqlConnection con, MovieItem movie)
        {
            try
            {
                string sql = String.Format(@"INSERT INTO Movie (Name, Original_Name, Length, Director,
                                                                             Writers, Stars, Storyline, Trailer, Music, 
                                                                             Cinematography, Rating, Id_Self)
                                                        OUTPUT INSERTED.Id_Movie
                                                        VALUES ('{0}', '{1}', {2}, '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}')",
movie.Name, movie.Original_Name, movie.Length, movie.Director, movie.Writers, movie.Stars,
movie.Storyline, movie.Trailer, movie.Music, movie.Cinematography, movie.Rating, movie.Id_Movie);

                SqlCommand sqlCommand = new SqlCommand(sql, con);
                sqlCommand.ExecuteNonQuery();

                AddShowsToMovie(con, movie);

            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message + " | at AddNewMovie, skipping movie");
            }
            
        }

        private static void AddShowsToMovie(SqlConnection con, MovieItem movie)
        {
            try
            {
                string sql = "SELECT Id_Movie from Movie Where Name='" + movie.Name + "'";
                SqlCommand sqlCommand = new SqlCommand(sql, con);
                using (SqlDataReader updateReader = sqlCommand.ExecuteReader())
                {
                    while (updateReader.Read())
                    {
                        foreach (var movieShow in movie.Shows)
                        {
                            sql = String.Format(
                                @"INSERT INTO Show (Id_Cinema, Id_Movie, ShowDate, Start, Room, is3D, Language)
                                                            Values ({0},{1},'{2}','{3}','{4}',{5},'{6}')",
                                movieShow.Id_Cinema, updateReader.GetInt32(0), null,
                                movieShow.Start, movieShow.Room, movieShow.is3D ? 1 : 0, movieShow.Language);

                            sqlCommand = new SqlCommand(sql, con);
                            sqlCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message + " | Skipped adding shows to " + movie.Original_Name + "||" + e.StackTrace);
            }
        }
    }
}
