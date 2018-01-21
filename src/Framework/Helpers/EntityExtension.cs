using System;
using System.Net;
using PoeHUD.Controllers;
using PoeHUD.Models;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;

namespace PoeHUD.Framework.Helpers
{
    public static class EntityExtension
    {
        public static float DistanceFromPlayer(this EntityWrapper entity)
        {
            var player = GameController.Instance.Player;
            var distance = entity.DistanceRender(player);
            return distance;
        }
        
        public static float GridDistanceFromPlayer(this EntityWrapper entity)
        {
            var player = GameController.Instance.Player;
            var distance = entity.DistanceGrid(player);
            return (float) distance;
        }

        public static float GridDistanceFromPlayer(this Entity e)
        {
            var player = GameController.Instance.Player;
            var distance = e.DistanceGrid(player);
            return (float) distance;
        }
        public static float DistanceFromPlayer(this Entity e)
        {
            var player = GameController.Instance.Player;
            var distance = e.DistanceRender(player);
            return (float) distance;
        }



        public static float DistanceRender(this Entity from, Entity to)
        {
            var fromRender = from.GetComponent<Render>();
            var toRender = to.GetComponent<Render>();
            var distance =  Math.Sqrt(Math.Pow(fromRender.Pos.X - toRender.Pos.X, 2) + Math.Pow(fromRender.Pos.Y - toRender.Pos.Y, 2));
            return (float) distance;
        }
        public static float DistanceRender(this Entity from, EntityWrapper to)
        {
            var fromRender = from.GetComponent<Render>();
            var toRender = to.GetComponent<Render>();
            var distance =  Math.Sqrt(Math.Pow(fromRender.Pos.X - toRender.Pos.X, 2) + Math.Pow(fromRender.Pos.Y - toRender.Pos.Y, 2));
            return (float) distance;
        }
        
        public static float DistanceRender(this EntityWrapper from, Entity to)
        {
            var fromRender = from.GetComponent<Render>();
            var toRender = to.GetComponent<Render>();
            var distance =  Math.Sqrt(Math.Pow(fromRender.Pos.X - toRender.Pos.X, 2) + Math.Pow(fromRender.Pos.Y - toRender.Pos.Y, 2));
            return (float) distance;
        }
        public static float DistanceRender(this EntityWrapper from, EntityWrapper to)
        {
            var fromRender = from.GetComponent<Render>();
            var toRender = to.GetComponent<Render>();
            var distance =  Math.Sqrt(Math.Pow(fromRender.Pos.X - toRender.Pos.X, 2) + Math.Pow(fromRender.Pos.Y - toRender.Pos.Y, 2));
            return (float) distance;
        }
        public static float DistanceGrid(this Entity from, Entity to)
        {
            var fromRender = from.GetComponent<Positioned>();
            var toRender = to.GetComponent<Positioned>();
            var distance =  Math.Sqrt(Math.Pow(fromRender.GridX - toRender.GridX, 2) + Math.Pow(fromRender.GridY - toRender.GridY, 2));
            return (float) distance;
        }
        public static float DistanceGrid(this Entity from, EntityWrapper to)
        {
            var fromRender = from.GetComponent<Positioned>();
            var toRender = to.GetComponent<Positioned>();
            var distance =  Math.Sqrt(Math.Pow(fromRender.GridX - toRender.GridX, 2) + Math.Pow(fromRender.GridY - toRender.GridY, 2));
            return (float) distance;
        }
        public static float DistanceGrid(this EntityWrapper from, Entity to)
        {
            var fromRender = from.GetComponent<Positioned>();
            var toRender = to.GetComponent<Positioned>();
            var distance =  Math.Sqrt(Math.Pow(fromRender.GridX - toRender.GridX, 2) + Math.Pow(fromRender.GridY - toRender.GridY, 2));
            return (float) distance;
        }
        public static float DistanceGrid(this EntityWrapper from, EntityWrapper to)
        {
            var fromRender = from.GetComponent<Positioned>();
            var toRender = to.GetComponent<Positioned>();
            var distance =  Math.Sqrt(Math.Pow(fromRender.GridX - toRender.GridX, 2) + Math.Pow(fromRender.GridY - toRender.GridY, 2));
            return (float) distance;
        }
    }
}