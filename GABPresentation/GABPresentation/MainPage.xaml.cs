using GABPresentation.Models;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Plugin.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace GABPresentation
{
    public partial class MainPage : ContentPage
    {
        private const string ApiKey = "fb637bdd0cf54c8180a6d8ab05d98a78";
        private string _idGrupo;
        private FaceServiceClient _clienteFace;
        private List<MSP> _listaMSPs;

        public MainPage()
        {
            InitializeComponent();

            //  Crea lista de personas para entrenar el modelo
            //  (normalmente extraída de una base de datos externa)
            _listaMSPs = new List<MSP>()
            {
                new MSP()
                {
                    Nombre = "Javier Escobar",
                    FotoUrl = "http://javierescobar.net/content/images/2016/07/Foto-1.PNG"
                },
                new MSP()
                {
                    Nombre = "Ian Brossard",
                    FotoUrl = "http://blog.ue.edu.pe/wp-content/uploads/2011/02/Ian-Paul-Brossard.jpg"
                },
                new MSP()
                {
                    Nombre = "Emily Mitacc",
                    FotoUrl = "https://pbs.twimg.com/profile_images/574775250346340353/XyoJc-vu.jpeg"
                },
                new MSP()
                {
                    Nombre = "Raian Bocanegra",
                    FotoUrl = "https://media.licdn.com/mpr/mpr/shrinknp_200_200/AAEAAQAAAAAAAAexAAAAJDY1OWZmMTY1LTkxODMtNDc0ZS05OGU5LTFmMjU4YTg4YTZhZQ.jpg"
                },
                new MSP()
                {
                    Nombre = "Melanie Pilco",
                    FotoUrl = "http://lima.azurebootcamp.net/agenda/image/GWAB/Staff/melaniepilco.jpg"
                }
            };

            RegisterMSPs();
        }

        private async void RegisterMSPs()
        {
            boton.IsEnabled = false;

            _clienteFace = new FaceServiceClient(ApiKey);



            #region PASO 1

            labelResultado.Text = "Creando grupo...";
            _idGrupo = Guid.NewGuid().ToString();
            await _clienteFace.CreatePersonGroupAsync(_idGrupo, "MSPs");

            #endregion



            #region PASO 2

            labelResultado.Text = "Agregando personas a grupo...";
            foreach (var msp in _listaMSPs)
            {
                var personaCreada = await _clienteFace.CreatePersonAsync(_idGrupo, msp.Nombre);
                await _clienteFace.AddPersonFaceAsync(_idGrupo, personaCreada.PersonId, msp.FotoUrl);
            }

            #endregion



            #region PASO 3

            labelResultado.Text = "Entrenando modelo...";
            await _clienteFace.TrainPersonGroupAsync(_idGrupo);

            #endregion



            labelResultado.Text = string.Empty;
            boton.IsEnabled = true;
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            boton.IsEnabled = false;

            try
            {
                await CrossMedia.Current.Initialize();
                var photo = await CrossMedia.Current.PickPhotoAsync();

                #region Obtener foto desde cámara
                //if (CrossMedia.Current.IsCameraAvailable)
                //{
                //    photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                //    {
                //        Directory = "MSPs",
                //        Name = "msp.jpg"
                //    });
                //}
                //else
                //{
                //    photo = await CrossMedia.Current.PickPhotoAsync();
                //}
                #endregion
                #region
                if (photo == null)
                    return;

                labelResultado.Text = "Verificando...";
                imagen.Source = photo.Path;
                #endregion



                #region PASO 4

                //  Cargar imagen a Cognitive Services
                using (var stream = photo.GetStream())
                {
                    //  Detecta rostros en la imagen
                    Face[] rostros = await _clienteFace.DetectAsync(stream);
                    if (rostros.Length == 0)
                    {
                        labelResultado.Text = "No se encontró ningún rostro.";
                        return;
                    }

                    //  Selecciona solo los id de los rostros detectados
                    var idsRostros = rostros.Select(rostro => rostro.FaceId).ToArray();

                    //  Pregunta al servicio por las caras detectadas
                    var resultadoIdentificacion = await _clienteFace.IdentifyAsync(_idGrupo, idsRostros);

                    //  Verifica si se identificó a alguna persona en el grupo
                    var candidatos = resultadoIdentificacion[0].Candidates;
                    if (candidatos.Length == 0)
                    {
                        labelResultado.Text = "No se reconoció a ningún MSP";
                        return;
                    }

                    //  Trae información de la persona identificada
                    var persona = await _clienteFace.GetPersonAsync(_idGrupo, candidatos[0].PersonId);

                    labelResultado.Text = $"¡Hola, {persona.Name}!";
                }

                #endregion


            }
            catch (FaceAPIException fex)
            {
                await DisplayAlert("Alerta", fex.ErrorMessage, "OK");
                labelResultado.Text = "Algo salió mal";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Alerta", ex.Message, "OK");
                labelResultado.Text = "Algo salió mal";
            }
            finally
            {
                boton.IsEnabled = true;
            }
        }

    }
}
