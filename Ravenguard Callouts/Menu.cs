using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ravenguard_Callouts
{
    public static class EntryPoint
    {
        private static UIMenu mainMenu;
        private static UIMenu vehicleSelect;
        private static MenuPool _menuPool;
        private static UIMenuItem navigateToSelectorMenuItem;
        private static UIMenuListItem modelList;
        private static UIMenu weaponSelect;
        private static UIMenuListItem weapList;

        public static void Main()
        {
            _menuPool = new MenuPool();
            mainMenu = new UIMenu("Main Menu", "");
            _menuPool.Add(mainMenu);            

            vehicleSelect = new UIMenu("Vehicle Select Menu", "");
            _menuPool.Add(vehicleSelect);

            navigateToSelectorMenuItem = new UIMenuItem("Vehicle Menu");
            mainMenu.AddItem(navigateToSelectorMenuItem);
            mainMenu.BindMenuToItem(vehicleSelect, navigateToSelectorMenuItem);
            vehicleSelect.ParentMenu = mainMenu;

            List<dynamic> listWithModels = new List<dynamic>()
            {
                "POLICE", "POLICE 2", "POLICE3", "POLICE4", "SHERIFF", "SHERIFF2"
            };
            modelList = new UIMenuListItem("Model", listWithModels, 0);
            vehicleSelect.AddItem(modelList);

            weaponSelect = new UIMenu("Weapons", "");
            _menuPool.Add(weaponSelect);
            navigateToSelectorMenuItem = new UIMenuItem("Weapon Menu");
            weaponSelect.AddItem(navigateToSelectorMenuItem);
            weaponSelect.BindMenuToItem(weaponSelect, navigateToSelectorMenuItem);
            weaponSelect.ParentMenu = mainMenu;

            List<dynamic> weaponList = new List<dynamic>()
            {
                "WEAPON_COMBATPISTOL", "WEAPON_NIGHTSTICK", "WEAPON_STUNGUN", "WEAPON_PUMPSHOTGUN", "WEAPON_CARBINERIFLE"
            };
            weapList = new UIMenuListItem("Police Weapons", weaponList, 0);
            weaponSelect.AddItem(weapList);

            mainMenu.RefreshIndex();
            vehicleSelect.RefreshIndex();
            weaponSelect.RefreshIndex();
            mainMenu.MouseControlsEnabled = false;
            mainMenu.AllowCameraMovement = true;
            vehicleSelect.MouseControlsEnabled = false;
            vehicleSelect.AllowCameraMovement = true;
            MainLogic();
            GameFiber.Hibernate();
        }
        public static void MainLogic()
        {
            GameFiber.StartNew(delegate
             {
                 while (true)
                 {
                     GameFiber.Yield();
                     if (Game.IsKeyDown(Keys.F5))
                     {
                         mainMenu.Visible = !mainMenu.Visible;
                     }
                     _menuPool.ProcessMenus();
                 }
             });
                                   
             
        }
    }
}
