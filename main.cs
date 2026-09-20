using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

using GTA;
using GTA.UI;
using GTA.Graphics;

namespace EnhancedVehicleLightingControls
{
    public class Main : Script
    {
   
        Keys sirenToggleKey     = Keys.Tab, 
        holdSiren                   = Keys.J,
        beamToggleKey           = Keys.CapsLock,
        interiorLightToggleKey  = Keys.I,
        leftIndicatorKey        = Keys.Left,
        rightIndicatorKey       = Keys.Right,
        hazardsKey              = Keys.Down;

        GTA.Control sirenToggleButton   = GTA.Control.ScriptPadDown,
        beamToggleButton                = GTA.Control.ScriptRLeft,
        interiorLightToggleButton       = GTA.Control.ScriptRUp,
        leftIndicatorButton             = GTA.Control.ScriptPadLeft,
        rightIndicatorButton            = GTA.Control.ScriptPadRight,
        hazardsButton                   = GTA.Control.ScriptPadUp, 
        modifierButton                  = GTA.Control.ScriptLB;

        private Dictionary<Keys,Action> kbKeyActions;

        /***
            IndicatorsManagment
        */
        private bool hasTurnedRight = false;
        private bool hasTurnedLeft = false;
        private const float TurnThreshold = 15.0f;
        private const float ResetThreshold = 5.0f;
        /***/

        /***
            QuickIndicatorManagment
        */
        private long indicatorSecondEllapsedTime = 5;
        private long currentTimeStamp = -1;

        private static readonly int RESET_DIRECTION   = 0x00;
        private static readonly int RIGHT_DIRECTION   = 0x01;
        private static readonly int LEFT_DIRECTION    = 0x02;
        /***/

        public Main()
        {
            this.Tick       += OnInit;
            this.KeyDown    += OnKeyDown;
            this.KeyUp      += OnKeyUp;

            #region Controls
            try{
                if(File.Exists("scripts\\EVLC_Settings.ini")){

                    ScriptSettings config = ScriptSettings.Load("scripts\\EVLC_Settings.ini");

                    #region Keys
                    sirenToggleKey          = config.GetValue<Keys>("Emergency Vehicles", "Siren_Toggle_Key", Keys.Tab);
                    holdSiren               = config.GetValue<Keys>("Emergency Vehicles", "Siren_hold_Key", Keys.J);
                    beamToggleKey           = config.GetValue<Keys>("Headlights", "Beam_Toggle_Key", Keys.CapsLock);
                    interiorLightToggleKey  = config.GetValue<Keys>("Interior", "Interior_Light_Toggle_Key", Keys.I);
                    leftIndicatorKey        = config.GetValue<Keys>("Indicators", "Left_Indicator_key", Keys.Left);
                    rightIndicatorKey       = config.GetValue<Keys>("Indicators", "Right_Indicator_Key", Keys.Right);
                    hazardsKey              = config.GetValue<Keys>("Indicators", "Hazard_Lights_Key", Keys.Down);
                    #endregion

                    #region Buttons
                    sirenToggleButton           = config.GetValue<GTA.Control>("Emergency Vehicles", "Siren_Toggle_Button", GTA.Control.ScriptPadDown);
                    beamToggleButton            = config.GetValue<GTA.Control>("Headlights", "Beam_Toggle_Button", GTA.Control.ScriptRLeft);
                    leftIndicatorButton         = config.GetValue<GTA.Control>("Indicators", "Left_Indicator_Button", GTA.Control.ScriptPadLeft);
                    rightIndicatorButton        = config.GetValue<GTA.Control>("Indicators", "Right_Indicator_Button", GTA.Control.ScriptPadRight);
                    hazardsButton               = config.GetValue<GTA.Control>("Indicators", "Hazard_Lights_Button", GTA.Control.ScriptPadUp);
                    modifierButton              = config.GetValue<GTA.Control>("Mod Settings", "Modifier_Button", GTA.Control.ScriptLB);
                    interiorLightToggleButton   = config.GetValue<GTA.Control>("Interior", "Interior_Light_Toggle_Button", GTA.Control.ScriptRUp);
                    #endregion
                }
            }catch{
                File.WriteAllText("scripts\\EVLC.log","Configuration via 'EVLC_Settings.ini' failed !");
            }

            kbKeyActions = new Dictionary<Keys, Action>{
              {rightIndicatorKey, ToggleRightIndicator},
              {leftIndicatorKey,ToggleLeftIndicator},
              {hazardsKey, ToggleHazards},
              {interiorLightToggleKey, ToggleInteriorLights},
              {beamToggleKey, ToggleFullBeams},
              {sirenToggleKey, ToggleSiren}
            };
            #endregion
        }

        private void OnInit(object sender, EventArgs e){

            if(ObjectIsNull(GetPlayer())) return;

            Wait(2000);

            string modName      = "Enhanced Vehicle Lighting Controls";
            string version      = "Release v1.0.0";
            string developer    = "MccDev260";
            Notification.PostMessageText($"{version} loaded !", new TextureAsset("CHAR_YOUTUBE", "CHAR_YOUTUBE"), false, FeedTextIcon.Message, developer, modName);

            this.Tick -= OnInit;
            this.Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e){

            if (Game.LastInputMethod == InputMethod.GamePad)GamePad();

            /**PROC*/
            IndicatorsManagment();
            QuickIndicatorManagment();
        }


        private void OnAborted(object sender, EventArgs e){
            this.Tick       -= OnTick;
            this.KeyDown    -= OnKeyDown;
            this.KeyUp      -= OnKeyUp;
        }

        #region Proc
        private void IndicatorsManagment(){

            if(!IsInVehicle||!HasIndicatorOn||IsQuickIndicator) return;
    
                
            if(IsRightIndicatorOn){

                    if(GetVehicle().SteeringAngle < -TurnThreshold ) hasTurnedRight = true;
                    else if(hasTurnedRight && Math.Abs(GetVehicle().SteeringAngle) < ResetThreshold){
                        ToggleRightIndicator();
                        hasTurnedRight = false;
                    }

            }else{
                hasTurnedRight = false;
            }
            if(IsLeftIndicatorOn){

                if(GetVehicle().SteeringAngle > TurnThreshold ) hasTurnedLeft = true;
                else if(hasTurnedLeft && Math.Abs(GetVehicle().SteeringAngle) < ResetThreshold){
                    ToggleLeftIndicator();
                    hasTurnedLeft = false;
                }

            }else{
                hasTurnedLeft = false;
            }


        }

        private void QuickIndicatorManagment(){

            if(!IsInVehicle||!HasIndicatorOn||!IsQuickIndicator) return;
            if(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - currentTimeStamp >= indicatorSecondEllapsedTime ){
                QuickIndicator( IsRightIndicatorOn? RIGHT_DIRECTION : LEFT_DIRECTION, false);
            }    

        }

        #endregion

        #region Input
        private void OnKeyDown(object sender, KeyEventArgs e)
        {   
            if (GetPlayer().IsInVehicle()){
              
                /*Toggle*/
                if(kbKeyActions.TryGetValue(e.KeyCode, out var action)) action();
                /*Others KbAction*/
                if(e.KeyCode==holdSiren) HoldSiren(true);
                /**Hooks emergency GTA key*/
                if(e.KeyCode==Keys.E&&GetVehicle().HasSiren) HazardsState(!GetVehicle().IsSirenActive);

                if(e.KeyCode==Keys.NumPad6) QuickIndicator(RIGHT_DIRECTION,true);
                if(e.KeyCode==Keys.NumPad5) QuickIndicator(RESET_DIRECTION,false);
                if(e.KeyCode==Keys.NumPad4) QuickIndicator(LEFT_DIRECTION,true);

            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e){

            if (GetPlayer().IsInVehicle()){
                if(e.KeyCode==holdSiren) HoldSiren(false);
            }

        }
        
        private void GamePad()
        {
            if (!IsInVehicle)
                return;

            if (Game.IsControlPressed(modifierButton))
            {
        
                // Disable all player controls except for some driving functions.
                Game.DisableAllControlsThisFrame();
                Game.EnableControlThisFrame(GTA.Control.VehicleAccelerate);
                Game.EnableControlThisFrame(GTA.Control.VehicleBrake);
                Game.EnableControlThisFrame(GTA.Control.VehicleHorn);
                Game.EnableControlThisFrame(GTA.Control.VehicleLookBehind);
                Game.EnableControlThisFrame(GTA.Control.LookLeftRight);
                Game.EnableControlThisFrame(GTA.Control.LookUpDown);
                Game.EnableControlThisFrame(GTA.Control.VehicleMoveLeftRight);

                if (Game.IsControlJustReleased(sirenToggleButton))
                    ToggleSiren();

                if (Game.IsControlJustReleased(beamToggleButton))
                    ToggleFullBeams();

                if (Game.IsControlJustPressed(interiorLightToggleButton))
                    ToggleInteriorLights();

                if (Game.IsControlJustPressed(leftIndicatorButton))
                    ToggleLeftIndicator();

                if (Game.IsControlJustPressed(rightIndicatorButton))
                    ToggleRightIndicator();

                if (Game.IsControlJustPressed(hazardsButton))
                    ToggleHazards();

            }else if (Game.IsControlJustReleased(modifierButton)){
                Game.EnableAllControlsThisFrame();
            }else{
                /**Hooks emergency GTA game controller*/
                if(Game.IsControlJustPressed(GTA.Control.VehicleHorn)&&GetVehicle().HasSiren) HazardsState(!GetVehicle().IsSirenActive);

                if (Game.IsControlPressed(GTA.Control.ScriptRDown)){
                    Game.DisableControlThisFrame(GTA.Control.VehicleDuck);
                }

                if(Game.IsControlJustPressed(GTA.Control.ScriptRDown))HoldSiren(true);
                else if(Game.IsControlJustReleased(GTA.Control.ScriptRDown))HoldSiren(false);

                if(Game.IsControlJustPressed(GTA.Control.VehicleBrake)) BreakLights(true);
                else if(Game.IsControlJustReleased(GTA.Control.VehicleBrake)) BreakLights(false);
            }
        }
        #endregion

        /***
        * <pre>
        *   Allows you to activate or deactivate the siren.
        * </pre>
        * @name     ToggleSiren
        * @return   void
        */
        private void ToggleSiren(){
            if(!IsInVehicle) return;
            ActiveSoundOfSiren(GetVehicle().IsSirenSilent);
        }

        /***
        * <pre>
        *   Enables or disables the siren; this function takes a
        *   boolean parameter that defines whether the sound is 
        *   activated or not.
        * </pre>
        * @name     ActiveSoundOfSiren
        * @params   bool sirenState
        * @return   void
        */
        private void ActiveSoundOfSiren(bool sirenState){
            if(!IsInVehicle) return;
            if(GetVehicle().HasSiren){
                if(!GetVehicle().IsSirenActive&&sirenState) GetVehicle().IsSirenActive = true;
                if(GetVehicle().IsSirenSilent == !sirenState ) return;
                GetVehicle().IsSirenSilent = !sirenState;
            }
        }

        /***
        * <pre>
        *   Allows hold siren with boolean param.
        * </pre>
        * @name     HoldSiren
        * @params   boolean state
        * @return   void
        */
        public void HoldSiren(bool state){
            if(!IsInVehicle) return;
            if(!GetVehicle().IsSirenActive&&state){
                GetVehicle().IsSirenActive = state;
                HazardsState(true);
            }
            ActiveSoundOfSiren(state);
        }
        
        /***
        * <pre>
        *   Allows Toggle Full Beams.
        * </pre>
        * @name     ToggleFullBeams
        * @return   void
        */
        private void ToggleFullBeams(){
            if(!IsInVehicle) return;
            if (GetVehicle().AreLightsOn)GetVehicle().AreHighBeamsOn = !GetVehicle().AreHighBeamsOn;
        }

        /***
        * <pre>
        *   Allows Toggle interior lights.
        * </pre>
        * @name     ToggleInteriorLights
        * @return   void
        */
        private void ToggleInteriorLights(){
            if(!IsInVehicle) return;
            GetVehicle().IsInteriorLightOn = !GetVehicle().IsInteriorLightOn;
        }

        #region Indicators

        /***
        * <pre>
        *   Indicates the status of the flashing lights.
        * </pre>
        * @name     HasHazards
        * @return   boolean
        */
        private bool AreHazardsOn => IsLeftIndicatorOn && IsRightIndicatorOn;

        /***
        * <pre>
        *   Indicates that one of the two flashing lights is on.
        * </pre>
        * @name     HasIndicatorOn
        * @return   boolean
        */
        private bool HasIndicatorOn => IsRightIndicatorOn||IsLeftIndicatorOn;

        /***
        * <pre>
        *   Indicates that one of the two flashing lights is on.
        * </pre>
        * @name     HasLeftIndicatorOn
        * @return   boolean
        */
        private bool IsLeftIndicatorOn => GetVehicle().IsLeftIndicatorLightOn;

        /***
        * <pre>
        *   Indicates whether the right turn signal is on.
        * </pre>
        * @name     HasRightIndicatorOn
        * @return   boolean
        */
        private bool IsRightIndicatorOn => GetVehicle().IsRightIndicatorLightOn;

        /***
        * <pre>
        *   Turns the hazard lights on or off.
        * </pre>
        * @name     ToggleHazards
        * @return   void
        */
        private void ToggleHazards(){HazardsState(!AreHazardsOn);}

        /***
        * <pre>
        *   Turns the hazard lights on or off with boolean param.
        * </pre>
        * @name     ToggleHazards
        * @params   boolean state
        * @return   void
        */
        private void HazardsState(bool state){SetIndicators(state, state);}

        /***
        * <pre>
        *   Activates or deactivates the right turn signal.
        * </pre>
        * @name     ToggleRightIndicator
        * @return   void
        */
        private void ToggleRightIndicator(){
            if (AreHazardsOn) return;
            SetIndicators(false, !IsRightIndicatorOn);
        }

        /***
        * <pre>
        *   Activates or deactivates the left turn signal.
        * </pre>
        * @name     ToggleLeftIndicator
        * @return   void
        */
        private void ToggleLeftIndicator(){
            if (AreHazardsOn) return;
            SetIndicators(!IsLeftIndicatorOn, false);
        }

        /***
        * <pre>
        *   This method allows you to define the state of the
        *   flashing lights; it takes two boolean parameters.
        *   When either parameter is true, the light is in the 
        *   "on" state.
        * </pre>
        * @name     SetIndicators
        * @params   boolean leftIndicator, boolean rightIndicator
        * @return   void
        */
        private void SetIndicators(bool leftIndicator = false, bool rightIndicator = false){
            if(!IsInVehicle) return;
            Vehicle vehicle = GetVehicle();
            vehicle.IsLeftIndicatorLightOn  = leftIndicator;
            vehicle.IsRightIndicatorLightOn = rightIndicator;
        }

        /***
        * <pre>
        *   Check if the flashing lights are in a temporary state.
        * </pre>
        * @name     IsQuickIndicator
        * @return   boolean
        */
        private bool IsQuickIndicator => currentTimeStamp > 0;

        /***
        * <pre>
        *   Allows you to temporarily activate the flashing lights.
        *   The direction parameter takes three different states:
        *
        *   - RIGHT_DIRECTION
        *   - LEFT_DIRECTION
        *   - RESET_DIRECTION
        * </pre>
        * @name     QuickIndicator
        * @params   int direction, boolean state
        * @return   boolean
        */
        private bool QuickIndicator(int direction, bool state){

            if(!IsInVehicle||AreHazardsOn) return false;
            if (IsQuickIndicator&&state || direction == RESET_DIRECTION) SetIndicators(false,false);

            if (direction == RIGHT_DIRECTION) SetIndicators(false,state);
            if (direction == LEFT_DIRECTION) SetIndicators(state,false);
            if (direction == RESET_DIRECTION ) state = false;

            currentTimeStamp = state ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : -1;

            return IsQuickIndicator;
        }


        private void BreakLights(bool state){
            if(!IsInVehicle||GetVehicle().IsSirenActive) return;
            GetVehicle().AreBrakeLightsOn = state;
        }
        #endregion

        /***/
        #region Helpers
        private Ped GetPlayer(){
            Ped currentPlayer = Game.Player.Character;

            if(currentPlayer!=null && currentPlayer.Exists()) return currentPlayer; 
            return null;
        }

        private Vehicle GetVehicle(Ped player){
            if(!ObjectIsNull(player)&&!ObjectIsNull(player.CurrentVehicle)&&player.CurrentVehicle.Exists()) return player.CurrentVehicle;
            return null;
        }

        private Vehicle GetVehicle(){ return GetVehicle(GetPlayer());}

        private bool IsInVehicle => !ObjectIsNull(GetVehicle(GetPlayer()));

        private bool ObjectIsNull(object o){ return o==null; }
        /***/
        #endregion

    }
}
