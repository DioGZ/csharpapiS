#nullable enable // Habilita las características de referencia nula en C#, permitiendo anotaciones y advertencias relacionadas con posibles valores nulos.
using System; // Importa el espacio de nombres que contiene tipos fundamentales como Exception, Console, etc.
using System.Collections.Generic; // Importa el espacio de nombres para colecciones genéricas como Dictionary.
using System.Data; // Importa el espacio de nombres para clases relacionadas con bases de datos.
using System.Data.Common; // Importa el espacio de nombres que define la clase base para proveedores de datos.
using Microsoft.AspNetCore.Authorization; // Importa el espacio de nombres para el control de autorización en ASP.NET Core.
using Microsoft.AspNetCore.Mvc; // Importa el espacio de nombres para la creación de controladores en ASP.NET Core.
using Microsoft.Extensions.Configuration; // Importa el espacio de nombres para acceder a la configuración de la aplicación.
using Microsoft.Data.SqlClient; // Importa el espacio de nombres necesario para trabajar con SQL Server y LocalDB.
using System.Linq; // Importa el espacio de nombres para operaciones de consulta con LINQ.
using System.Text.Json; // Importa el espacio de nombres para manejar JSON.
//using ProyectoBackendCsharp.Models; // Importa los modelos del proyecto.
using csharpapi.Services; // Importa los servicios del proyecto.
using BCrypt.Net; // Importa el espacio de nombres para trabajar con BCrypt para hashing de contraseñas.

namespace csharpapi.Controllers
{
    // Define la ruta base de la API usando variables dinámicas para mayor flexibilidad
    [Route("api/{nombreProyecto}/{nombreTabla}")]
    [ApiController] // Marca la clase como un controlador de API en ASP.NET Core.
    [Authorize] // Aplica autorización para que solo usuarios autenticados puedan acceder a estos endpoints.
    public class EntidadesController : ControllerBase
    {
        private readonly ControlConexion controlConexion; // Servicio para manejar la conexión a la base de datos.
        private readonly IConfiguration _configuration; // Configuración de la aplicación para obtener valores de appsettings.json.
        
        // Constructor que inyecta los servicios necesarios
        public EntidadesController(ControlConexion controlConexion, IConfiguration configuration)
        {
            this.controlConexion = controlConexion ?? throw new ArgumentNullException(nameof(controlConexion));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [AllowAnonymous] // Permite que cualquier usuario acceda a este endpoint, sin necesidad de autenticación.
        [HttpGet] // Define que este método responde a solicitudes HTTP GET.
        public IActionResult Listar(string nombreProyecto, string nombreTabla) // Método para listar los registros de una tabla específica.
        {
            // Verifica si el nombre de la tabla es nulo o vacío
            if (string.IsNullOrWhiteSpace(nombreTabla)) 
                return BadRequest("El nombre de la tabla no puede estar vacío.");

            try
            {
                var listaFilas = new List<Dictionary<string, object?>>(); // Lista para almacenar las filas obtenidas de la base de datos.
                string comandoSQL = $"SELECT * FROM {nombreTabla}"; // Consulta SQL para obtener todos los registros de la tabla.

                controlConexion.AbrirBd(); // Abre la conexión con la base de datos.
                var tablaResultados = controlConexion.EjecutarConsultaSql(comandoSQL, null); // Ejecuta la consulta y obtiene los datos en un DataTable.
                controlConexion.CerrarBd(); // Cierra la conexión para liberar recursos.

                // Recorre cada fila del resultado y la convierte en un diccionario clave-valor.
                foreach (DataRow fila in tablaResultados.Rows)
                {
                    var propiedadesFila = fila.Table.Columns.Cast<DataColumn>()
                        .ToDictionary(columna => columna.ColumnName, 
                                      columna => fila[columna] == DBNull.Value ? null : fila[columna]);
                    listaFilas.Add(propiedadesFila); // Agrega la fila convertida a la lista.
                }

                return Ok(listaFilas); // Devuelve la lista de registros en formato JSON con código de estado 200 (OK).
            }
            catch (Exception ex)
            {
                int codigoError;
                string mensajeError;

                if (ex is SqlException sqlEx)
                {
                    // Mapea códigos de error SQL a códigos HTTP
                    codigoError = sqlEx.Number switch
                    {
                        208 => 404, // Tabla no encontrada
                        547 => 409, // Violación de restricción (clave foránea)
                        2627 => 409, // Clave única duplicada
                        _ => 500 // Otros errores desconocidos
                    };
                    mensajeError = $"Error ({codigoError}): {sqlEx.Message}";
                }
                else
                {
                    codigoError = 500; // Error interno del servidor.
                    mensajeError = $"Error interno del servidor: {ex.Message}";
                }
                return StatusCode(codigoError, mensajeError); // Devuelve un mensaje de error con el código correspondiente.
            }
        }
    }
}
