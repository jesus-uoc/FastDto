using FastDTO;
using System.Text.Json;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var origen = new Persona { Nombre = "Juan", Edad = 30, OtraPropiedad = "hola" };
            var destino = new Persona2();
            destino.CopyFrom(origen);

            var origen2 = new Persona { OtraPropiedad = "hola" };
            var destino2 = new Persona2 { Nombre = "Juan", Edad = 30, OtraPropiedad = "hola" };
            destino2.CopyFrom(origen2);

            var origen3 = new Persona2 { Nombre = "Juan", Edad = 30, OtraPropiedad = "hola" };
            var destino3 = new Persona();
            destino3.CopyFrom(origen3);

            var origen4 = new Persona3 { Edad = 30, OtraPropiedad = "hola" };
            var destino4 = new Persona();
            destino4.CopyFrom(origen4);

            string jsonString = JsonSerializer.Serialize(destino4);
            var a = IPersonaExtensions.NewFrom<Persona>(origen4);

            List<Persona> lista = new();
            var lista2 = lista.NewListFrom<Persona2>();
            lista.Add(origen);
            lista.Add(origen);
            lista2 = lista.NewListFrom<Persona2>();

        }
    }
    [CopyByInterfaz]
    public interface IPersona : IPersona3
    {
        string Nombre { get; set; }
        int Edad { get; set; }
        int Edad5 { get; set; }
    }

    public interface IPersona3
    {
        string Nombre2 { get; set; }
        int Edad2 { get; set; }
    }

    public class Persona : IPersona
    {
        public string Nombre { get; set; }
        public int Edad { get; set; }
        public string OtraPropiedad { get; set; }
        public string Nombre2 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Edad2 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Edad5 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public class Persona2 : IPersona
    {
        public string Nombre { get; set; }
        public int Edad { get; set; }
        public string OtraPropiedad { get; set; }
        public string Nombre2 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Edad2 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Edad5 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public class Persona3 : IPersona
    {
        public string Nombre
        {
            get
            {
                return OtraPropiedad;
            }
            set
            {
                OtraPropiedad = value;
            }
        }
        public int Edad { get; set; }
        public string OtraPropiedad { get; set; }
        public string Nombre2 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Edad2 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Edad5 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }


}
