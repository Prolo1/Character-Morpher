using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HS2;

using KKAPI;
using KKAPI.MainGame;
using KKAPI.Utilities;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Chara;

using UnityEngine;
using UnityEngine.UI;


namespace HS2_CharaMorpher
{
    class CharaMorpherGUI
    {

        internal static void Initialize()
        {
            MakerAPI.RegisterCustomSubCategories += AddCharaMorpherMenu;
        }


        static int abmxIndex = 0;
        static List<MakerSlider> sliders = new List<MakerSlider>();
        private static void AddCharaMorpherMenu(object sender, RegisterSubCategoriesEvent e)
        {
            if(MakerAPI.GetMakerSex() != 1) return;


            var cfg = CharaMorpher.Instance.cfg;

            MakerCategory category = new MakerCategory(MakerConstants.Parameter.CategoryName, "Character Morpher");
            e.AddSubCategory(category);

            //Enables
            e.AddControl(new MakerText("Enablers", category, CharaMorpher.Instance));
            var enable = e.AddControl(new MakerToggle(category, "Enable", cfg.enable.Value, CharaMorpher.Instance));
            enable.BindToFunctionController<CharaMorpherController, bool>(
                (control) => cfg.enable.Value,
                (control, val) => { cfg.enable.Value = val; for(int a = 0; a < sliders.Count; ++a) sliders[a].ControlObject.SetActive(val); });

            var enableabmx = e.AddControl(new MakerToggle(category, "Enable ABMX", cfg.enableABMX.Value, CharaMorpher.Instance));
            enableabmx.BindToFunctionController<CharaMorpherController, bool>(
                (control) => cfg.enableABMX.Value,
                (control, val) => { cfg.enableABMX.Value = val; for(int a = abmxIndex; a < sliders.Count; ++a) sliders[a].ControlObject.SetActive(cfg.enable.Value && val); });


            e.AddControl(new MakerSeparator(category, CharaMorpher.Instance));
            e.AddControl(new MakerText("", category, CharaMorpher.Instance));//create space


            //Sliders
            {
                int index = 0;
                sliders.Clear();
                float min = -cfg.sliderExtents.Value * .01f, max = 1 + cfg.sliderExtents.Value * .01f;

                e.AddControl(new MakerText("Body Controls", category, CharaMorpher.Instance));
                sliders.Add(e.AddControl(new MakerSlider(category, "Overall Body", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                   (control) => CharaMorpherController.controls.body,
                    (control, val) => { CharaMorpherController.controls.body = val; control.MorphChangeUpdate(); });

                e.AddControl(new MakerSeparator(category, CharaMorpher.Instance));
                sliders.Add(e.AddControl(new MakerSlider(category, "Head", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.head,
                  (control, val) => { CharaMorpherController.controls.head = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Boobs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.boob,
                  (control, val) => { CharaMorpherController.controls.boob = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Butt", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.butt,
                  (control, val) => { CharaMorpherController.controls.butt = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Torso", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.torso,
                  (control, val) => { CharaMorpherController.controls.torso = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Arms", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.arm,
                  (control, val) => { CharaMorpherController.controls.arm = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Legs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.leg,
                  (control, val) => { CharaMorpherController.controls.leg = val; control.MorphChangeUpdate(); });



                e.AddControl(new MakerText("", category, CharaMorpher.Instance));//create space
                e.AddControl(new MakerText("Head Controls", category, CharaMorpher.Instance));

                sliders.Add(e.AddControl(new MakerSlider(category, "Overall Head", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                   (control) => CharaMorpherController.controls.face,
                    (control, val) => { CharaMorpherController.controls.face = val; control.MorphChangeUpdate(); });

                e.AddControl(new MakerSeparator(category, CharaMorpher.Instance));
                sliders.Add(e.AddControl(new MakerSlider(category, "Ears", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.ear,
                  (control, val) => { CharaMorpherController.controls.ear = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Eyes", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.eyes,
                  (control, val) => { CharaMorpherController.controls.eyes = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Mouth", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.mouth,
                  (control, val) => { CharaMorpherController.controls.mouth = val; control.MorphChangeUpdate(); });

                abmxIndex = index;//may use this later

                e.AddControl(new MakerText("", category, CharaMorpher.Instance));//create space
                e.AddControl(new MakerText("ABMX Body", category, CharaMorpher.Instance));

                sliders.Add(e.AddControl(new MakerSlider(category, "Body", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxBody,
                  (control, val) => { CharaMorpherController.controls.abmxBody = val; control.MorphChangeUpdate(); });

                e.AddControl(new MakerSeparator(category, CharaMorpher.Instance));
                sliders.Add(e.AddControl(new MakerSlider(category, "Boobs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxBoobs,
                  (control, val) => { CharaMorpherController.controls.abmxBoobs = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Butt", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxButt,
                  (control, val) => { CharaMorpherController.controls.abmxButt = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Torso", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxTorso,
                  (control, val) => { CharaMorpherController.controls.abmxTorso = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Arms", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxArms,
                  (control, val) => { CharaMorpherController.controls.abmxArms = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Hands", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxHands,
                  (control, val) => { CharaMorpherController.controls.abmxHands = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Legs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxLegs,
                  (control, val) => { CharaMorpherController.controls.abmxLegs = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Feet", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxFeet,
                  (control, val) => { CharaMorpherController.controls.abmxFeet = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Genitals", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxGenitals,
                  (control, val) => { CharaMorpherController.controls.abmxGenitals = val; control.MorphChangeUpdate(); });





                e.AddControl(new MakerText("", category, CharaMorpher.Instance));//create space
                e.AddControl(new MakerText("ABMX Head", category, CharaMorpher.Instance));

                sliders.Add(e.AddControl(new MakerSlider(category, "Head", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxHead,
                  (control, val) => { CharaMorpherController.controls.abmxHead = val; control.MorphChangeUpdate(); });

                e.AddControl(new MakerSeparator(category, CharaMorpher.Instance));
                sliders.Add(e.AddControl(new MakerSlider(category, "Ears", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxEars,
                  (control, val) => { CharaMorpherController.controls.abmxEars = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Eyes", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxEyes,
                  (control, val) => { CharaMorpherController.controls.abmxEyes = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Mouth", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxMouth,
                  (control, val) => { CharaMorpherController.controls.abmxMouth = val; control.MorphChangeUpdate(); });

                sliders.Add(e.AddControl(new MakerSlider(category, "Hair", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher.Instance)));
                sliders.Last().BindToFunctionController<CharaMorpherController, float>(
                 (control) => CharaMorpherController.controls.abmxHair,
                  (control, val) => { CharaMorpherController.controls.abmxHair = val; control.MorphChangeUpdate(); });
            }

            e.AddControl(new MakerText("", category, CharaMorpher.Instance));//create space
            e.AddControl(new MakerSeparator(category, CharaMorpher.Instance));

            //initial slider visability
            for(int a = 0; a < sliders.Count; ++a)
            {
                var obj = sliders[a].ControlObject;
                if(obj) obj.SetActive(cfg.enable.Value);
            }
            for(int a = abmxIndex; a < sliders.Count; ++a)
            {
                var obj = sliders[a].ControlObject;
                if(obj) obj.SetActive(cfg.enable.Value && cfg.enableABMX.Value);
            }

            //Buttons
            var saveDefaultsButton = e.AddControl(new MakerButton("Save Default", category, CharaMorpher.Instance));
            saveDefaultsButton.OnClick.AddListener(
                () =>
                {
                    int count = 0;
                    foreach(var def in cfg.defaults)
                        def.Value = sliders[count++].Value * 100f;
                });

            var loadDefaultsButton = e.AddControl(new MakerButton("Load Default", category, CharaMorpher.Instance));
            loadDefaultsButton.OnClick.AddListener(
               () =>
               {
                   int count = 0;
                   foreach(var slider in sliders)
                       slider.Value = cfg.defaults[count++].Value * .01f;
               });
        }
        /*
                private void OpenFolder(string folderPath)
                {
                    if(Directory.Exists(folderPath))
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            Arguments = folderPath,
                            FileName = "explorer.exe",

                        };

                        Process.Start(startInfo);
                    }
                    else
                    {
                        //Error here
                    }
                }
        */
    }
}
