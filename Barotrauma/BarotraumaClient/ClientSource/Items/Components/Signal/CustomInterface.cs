﻿using Barotrauma.Networking;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Barotrauma.Items.Components
{
    partial class CustomInterface
    {
        private readonly List<GUIComponent> uiElements = new List<GUIComponent>();
        private GUILayoutGroup uiElementContainer;

        private Point ElementMaxSize => new Point(uiElementContainer.Rect.Width, (int)(65 * GUI.yScale));

        partial void InitProjSpecific()
        {
            CreateGUI();
        }

        protected override void CreateGUI()
        {
            uiElements.Clear();
            var visibleElements = customInterfaceElementList.Where(ciElement => !string.IsNullOrEmpty(ciElement.Label));
            uiElementContainer = new GUILayoutGroup(new RectTransform(GuiFrame.Rect.Size - GUIStyle.ItemFrameMargin, GuiFrame.RectTransform, Anchor.Center)
            {
                AbsoluteOffset = GUIStyle.ItemFrameOffset
            },
                childAnchor: customInterfaceElementList.Count > 1 ? Anchor.TopCenter : Anchor.Center)
            {
                RelativeSpacing = 0.05f,
                Stretch = visibleElements.Count() > 2,
            };

            float elementSize = Math.Min(1.0f / visibleElements.Count(), 1);
            foreach (CustomInterfaceElement ciElement in visibleElements)
            {
                if (ciElement.HasPropertyName)
                {
                    var layoutGroup = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, elementSize), uiElementContainer.RectTransform), isHorizontal: true)
                    {
                        RelativeSpacing = 0.02f,
                        UserData = ciElement
                    };
                    new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), layoutGroup.RectTransform), 
                        TextManager.Get(ciElement.Label).Fallback(ciElement.Label));
                    if (!ciElement.IsIntegerInput)
                    {
                        var textBox = new GUITextBox(new RectTransform(new Vector2(0.5f, 1.0f), layoutGroup.RectTransform), ciElement.Signal, style: "GUITextBoxNoIcon")
                        {
                            OverflowClip = true,
                            UserData = ciElement,
                            MaxTextLength = ciElement.MaxTextLength
                        };
                        //reset size restrictions set by the Style to make sure the elements can fit the interface
                        textBox.RectTransform.MinSize = textBox.Frame.RectTransform.MinSize = new Point(0, 0);
                        textBox.RectTransform.MaxSize = textBox.Frame.RectTransform.MaxSize = new Point(int.MaxValue, int.MaxValue);
                        textBox.OnDeselected += (tb, key) =>
                        {
                            if (GameMain.Client == null)
                            {
                                TextChanged(tb.UserData as CustomInterfaceElement, textBox.Text);
                            }
                            else
                            {
                                item.CreateClientEvent(this);
                            }
                        };

                        textBox.OnEnterPressed += (tb, text) =>
                        {
                            tb.Deselect();
                            return true;
                        };
                        uiElements.Add(textBox);
                    }
                    else
                    {
                        int.TryParse(ciElement.Signal, out int signal);
                        var numberInput = new GUINumberInput(new RectTransform(new Vector2(0.5f, 1.0f), layoutGroup.RectTransform), GUINumberInput.NumberType.Int)
                        {
                            UserData = ciElement,
                            MinValueInt = ciElement.NumberInputMin,
                            MaxValueInt = ciElement.NumberInputMax,
                            IntValue = Math.Clamp(signal, ciElement.NumberInputMin, ciElement.NumberInputMax)
                        };
                        //reset size restrictions set by the Style to make sure the elements can fit the interface
                        numberInput.RectTransform.MinSize = numberInput.LayoutGroup.RectTransform.MinSize = new Point(0, 0);
                        numberInput.RectTransform.MaxSize = numberInput.LayoutGroup.RectTransform.MaxSize = new Point(int.MaxValue, int.MaxValue);
                        numberInput.OnValueChanged += (ni) =>
                        {
                            if (GameMain.Client == null)
                            {
                                ValueChanged(ni.UserData as CustomInterfaceElement, ni.IntValue);
                            }
                            else
                            {
                                item.CreateClientEvent(this);
                            }
                        };
                        uiElements.Add(numberInput);
                    }
                }
                else if (ciElement.ContinuousSignal)
                {
                    var tickBox = new GUITickBox(new RectTransform(new Vector2(1.0f, elementSize), uiElementContainer.RectTransform)
                    {
                        MaxSize = ElementMaxSize
                    }, TextManager.Get(ciElement.Label).Fallback(ciElement.Label))
                    {
                        UserData = ciElement
                    };
                    tickBox.OnSelected += (tBox) =>
                    {
                        if (GameMain.Client == null)
                        {
                            TickBoxToggled(tBox.UserData as CustomInterfaceElement, tBox.Selected);
                        }
                        else
                        {
                            item.CreateClientEvent(this);
                        }
                        return true;
                    };
                    //reset size restrictions set by the Style to make sure the elements can fit the interface
                    tickBox.RectTransform.MinSize = new Point(0, 0);
                    tickBox.RectTransform.MaxSize = new Point(int.MaxValue, int.MaxValue);
                    uiElements.Add(tickBox);
                }
                else
                {
                    var btn = new GUIButton(new RectTransform(new Vector2(1.0f, elementSize), uiElementContainer.RectTransform),
                        TextManager.Get(ciElement.Label).Fallback(ciElement.Label), style: "DeviceButton")
                    {
                        UserData = ciElement
                    };
                    btn.OnClicked += (_, userdata) =>
                    {
                        CustomInterfaceElement btnElement = userdata as CustomInterfaceElement;;
                        if (GameMain.Client == null)
                        {
                            ButtonClicked(btnElement);
                        }
                        else
                        {
                            item.CreateClientEvent(this, new EventData(btnElement));
                        }
                        return true;
                    };

                    //reset size restrictions set by the Style to make sure the elements can fit the interface
                    btn.RectTransform.MinSize = btn.Frame.RectTransform.MinSize = new Point(0, 0);
                    btn.RectTransform.MaxSize = btn.Frame.RectTransform.MaxSize = ElementMaxSize;

                    uiElements.Add(btn);
                }
            }
        }

        public override void CreateEditingHUD(SerializableEntityEditor editor)
        {
            base.CreateEditingHUD(editor);

            if (customInterfaceElementList.Count > 0) 
            { 
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(customInterfaceElementList[0]);
                PropertyDescriptor labelProperty = properties.Find("Label", false);
                PropertyDescriptor signalProperty = properties.Find("Signal", false);
                for (int i = 0; i < customInterfaceElementList.Count; i++)
                {
                    editor.CreateStringField(customInterfaceElementList[i],
                        new SerializableProperty(labelProperty),
                        customInterfaceElementList[i].Label, "Label #" + (i + 1), "");
                    editor.CreateStringField(customInterfaceElementList[i],
                        new SerializableProperty(signalProperty),
                        customInterfaceElementList[i].Signal, "Signal #" + (i + 1), "");
                }
            }
        }

        public void HighlightElement(int index, Color color, float duration, float pulsateAmount = 0.0f)
        {
            if (index < 0 || index >= uiElements.Count) { return; }
            uiElements[index].Flash(color, duration);

            if (pulsateAmount > 0.0f)
            {
                if (uiElements[index] is GUIButton button)
                {
                    button.Frame.Pulsate(Vector2.One, Vector2.One * (1.0f + pulsateAmount), duration);
                    button.Frame.RectTransform.SetPosition(Anchor.Center);
                }
                else
                {
                    uiElements[index].Pulsate(Vector2.One, Vector2.One * (1.0f + pulsateAmount), duration);
                }
            }
        }

        public override void UpdateHUD(Character character, float deltaTime, Camera cam)
        {
            bool elementVisibilityChanged = false;
            int visibleElementCount = 0;
            foreach (var uiElement in uiElements)
            {
                if (!(uiElement.UserData is CustomInterfaceElement element)) { continue; }
                bool visible = Screen.Selected == GameMain.SubEditorScreen || element.StatusEffects.Any() || element.HasPropertyName || (element.Connection != null && element.Connection.Wires.Any(w => w != null));
                if (visible) { visibleElementCount++; }
                if (uiElement.Visible != visible)
                {
                    uiElement.Visible = visible;
                    uiElement.IgnoreLayoutGroups = !uiElement.Visible;
                    elementVisibilityChanged = true;
                }
            }

            if (elementVisibilityChanged)
            {
                uiElementContainer.Stretch = visibleElementCount > 2;
                uiElementContainer.ChildAnchor = visibleElementCount > 1 ? Anchor.TopCenter : Anchor.Center;
                float elementSize = Math.Min(1.0f / visibleElementCount, 1);
                foreach (var uiElement in uiElements)
                {
                    uiElement.RectTransform.RelativeSize = new Vector2(1.0f, elementSize);
                }
                GuiFrame.Visible = visibleElementCount > 0;
                uiElementContainer.Recalculate();
            }
        }

        partial void UpdateLabelsProjSpecific()
        {
            for (int i = 0; i < labels.Length && i < uiElements.Count; i++)
            {
                if (uiElements[i] is GUIButton button)
                {
                    button.Text = CreateLabelText(i);
                    button.TextBlock.Wrap = button.Text.Contains(' ');
                }
                else if (uiElements[i] is GUITickBox tickBox)
                {
                    tickBox.Text = CreateLabelText(i);
                    tickBox.TextBlock.Wrap = tickBox.Text.Contains(' ');
                }
                else if (uiElements[i] is GUITextBox || uiElements[i] is GUINumberInput)
                {
                    var textBlock = uiElements[i].Parent.GetChild<GUITextBlock>();
                    textBlock.Text = CreateLabelText(i);
                    textBlock.Wrap = textBlock.Text.Contains(' ');
                }
            }

            LocalizedString CreateLabelText(int elementIndex)
            {
                return string.IsNullOrWhiteSpace(customInterfaceElementList[elementIndex].Label) ?
                    TextManager.GetWithVariable("connection.signaloutx", "[num]", (elementIndex + 1).ToString()) :
                    customInterfaceElementList[elementIndex].Label;
            }

            uiElementContainer.Recalculate();
            var textBlocks = new List<GUITextBlock>();
            foreach (GUIComponent element in uiElementContainer.Children)
            {
                if (element is GUIButton btn)
                {
                    if (btn.TextBlock.TextSize.Y > btn.Rect.Height - btn.TextBlock.Padding.Y - btn.TextBlock.Padding.W)
                    {
                        btn.RectTransform.RelativeSize = new Vector2(btn.RectTransform.RelativeSize.X, btn.RectTransform.RelativeSize.Y * 1.5f);
                    }
                    textBlocks.Add(btn.TextBlock);
                }
                else if (element is GUITickBox tickBox)
                {
                    textBlocks.Add(tickBox.TextBlock);
                }
                else if (element is GUILayoutGroup)
                {
                    textBlocks.Add(element.GetChild<GUITextBlock>());
                }
            }
            uiElementContainer.Recalculate();
            GUITextBlock.AutoScaleAndNormalize(textBlocks);
        }

        partial void UpdateSignalsProjSpecific()
        {
            for (int i = 0; i < signals.Length && i < uiElements.Count; i++)
            {
                if (uiElements[i] is GUITextBox tb)
                {
                    tb.Text = customInterfaceElementList[i].Signal;
                }
                else if (uiElements[i] is GUINumberInput ni)
                {
                    if (ni.InputType == GUINumberInput.NumberType.Int)
                    {
                        int.TryParse(customInterfaceElementList[i].Signal, out int value);
                        ni.IntValue = value;
                    }
                }
            }
        }

        public void ClientEventWrite(IWriteMessage msg, NetEntityEvent.IData extraData = null)
        {
            //extradata contains an array of buttons clicked by the player (or nothing if the player didn't click anything)
            for (int i = 0; i < customInterfaceElementList.Count; i++)
            {
                if (customInterfaceElementList[i].HasPropertyName)
                {
                    if (!customInterfaceElementList[i].IsIntegerInput)
                    {
                        msg.Write(((GUITextBox)uiElements[i]).Text);
                    }
                    else
                    {
                        msg.Write(((GUINumberInput)uiElements[i]).IntValue.ToString());
                    }
                }
                else if (customInterfaceElementList[i].ContinuousSignal)
                {
                    msg.Write(((GUITickBox)uiElements[i]).Selected);
                }
                else
                {
                    msg.Write(extraData is Item.ComponentStateEventData { ComponentData: EventData eventData } && eventData.BtnElement == customInterfaceElementList[i]);
                }
            }
        }

        public void ClientEventRead(IReadMessage msg, float sendingTime)
        {
            for (int i = 0; i < customInterfaceElementList.Count; i++)
            {
                if (customInterfaceElementList[i].HasPropertyName)
                {
                    if (!customInterfaceElementList[i].IsIntegerInput)
                    {
                        TextChanged(customInterfaceElementList[i], msg.ReadString());
                    }
                    else
                    {
                        int.TryParse(msg.ReadString(), out int value);
                        ValueChanged(customInterfaceElementList[i], value);
                    }
                }
                else
                {
                    bool elementState = msg.ReadBoolean();
                    if (customInterfaceElementList[i].ContinuousSignal)
                    {
                        ((GUITickBox)uiElements[i]).Selected = elementState;
                        TickBoxToggled(customInterfaceElementList[i], elementState);
                    }
                    else if (elementState)
                    {
                        ButtonClicked(customInterfaceElementList[i]);
                    }
                }
            }

            UpdateSignalsProjSpecific();
        }
    }
}
