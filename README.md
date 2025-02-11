# FastDto
Generador de codigo para copiar las propiedades del interfaz comun entre clases. 

Por ejemplo, crea una interfaz con las propiedades que quieran que se copien, crea las dos clases que implementen el interfaz y que quieres que se copien los datos de las interfaz. Solo se copiaran las propiedades del interfaz ignorando las demas propiedades de la clase, es bidireccional funciona tanto de una clase a otra como viceversa. Si quieres hacer una adaptacion de las propiedades que se van a copiar hazlo en la clase a la hora de implementar el interfaz.

Es muchisimo más rapido que automapper ya que no utiliza reflexion, y como solo se copian las propiedades del interfaz sabes exactamente el resultado de la copia.

Ejemplo de uso:
- Crea un proyecto de consola de Net
- añade el nuget FastDto a tu proyecto
- añade a program: using FastDTO;
- copia a tu program.cs:
  
```c#
internal class Program
{
    static void Main(string[] args)
    {
        var origen = new Persona { Nombre = "Juan", Edad = 30, OtraPropiedad = "hola" };
        var destino = new Persona2();
        destino.CopyFrom(origen);

        var origen2 = new Persona2 { Nombre = "Juan", Edad = 30, OtraPropiedad = "hola" };
        var destino2 = new Persona();
        destino2.CopyFrom(origen2);

        var origen3 = new Persona3 { Edad = 30, OtraPropiedad = "hola" };
        var destino3 = new Persona();
        destino3.CopyFrom(origen3);

        var origen4 = new Persona4 { Edad = 30, OtraPropiedad = "hola" };
        Persona destino4 = orgien3.NewFrom<Persona>();

        List<Persona> destino5 = origen3.NewListFrom<Persona>();
    }
}
[CopyByInterfaz]
public interface IPersona
{
    string Nombre { get; set; }
    int Edad { get; set; }
}

public class Persona : IPersona
{
    public string Nombre { get; set; }
    public int Edad { get; set; }
    public string OtraPropiedad { get; set; }
}

public class Persona2 : IPersona
{
    public string Nombre { get; set; }
    public int Edad { get; set; }
    public string OtraPropiedad { get; set; }
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
}
```
