// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Screens.Edit
{
    public partial class ImportHitSamplesDialog : PopupDialog
    {
        /// <summary>
        /// Delegate used to import hit samples from the source beatmap.
        /// </summary>
        public delegate void ImportHitSamples(BeatmapInfo beatmap);

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private FillFlowContainer settings = null!;

        public ImportHitSamplesDialog(ImportHitSamples importHitSamples, BeatmapInfo beatmap, string difficultyName)
        {
            HeaderText = EditorDialogsStrings.ImportHitSamplesDialogHeader(difficultyName);

            Icon = FontAwesome.Solid.Music;

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = DialogStrings.Confirm,
                    Action = () => importHitSamples(beatmap)
                },
                new PopupDialogCancelButton
                {
                    Text = DialogStrings.Cancel,
                    Action = () => { }
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager configManager)
        {
            LoadComponent(settings = new HitSamplesImportSettings(configManager));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            PopupSettings.Add(settings);
        }

        private partial class HitSamplesImportSettings : FillFlowContainer
        {
            private readonly OsuConfigManager config;
            private readonly List<SampleCopyMode> copyModes;

            public HitSamplesImportSettings(OsuConfigManager config)
            {
                this.config = config;
                copyModes = new List<SampleCopyMode> { SampleCopyMode.OverwriteAllSamples, SampleCopyMode.OverwriteDefinedSamples };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Origin = Anchor.TopCentre;
                Anchor = Anchor.TopCentre;
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                Add(new FillFlowContainer
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new FormDropdown<SampleCopyMode>
                        {
                            Caption = EditorDialogsStrings.HitSampleCopyMode,
                            Items = copyModes,
                            Current = config.GetBindable<SampleCopyMode>(OsuSetting.EditorSampleCopyMode),
                        },
                        new FormSliderBar<double>
                        {
                            Caption = EditorDialogsStrings.TemporalLeniency,
                            Current = config.GetBindable<double>(OsuSetting.EditorSampleCopyLeniency),
                            KeyboardStep = 1.0f,
                            TransferValueOnCommit = true,
                            TabbableContentContainer = this
                        },
                    }
                });

                Add(new FillFlowContainer<FormCheckBox>
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new[]
                    {
                        new FormCheckBox
                        {
                            Caption = EditorDialogsStrings.CopyHitSamples,
                            Current = config.GetBindable<bool>(OsuSetting.EditorCopySamples),
                        },
                        new FormCheckBox
                        {
                            Caption = EditorDialogsStrings.CopyBanks,
                            Current = config.GetBindable<bool>(OsuSetting.EditorCopyBanks),
                        },
                        new FormCheckBox
                        {
                            Caption = EditorDialogsStrings.CopyVolumes,
                            Current = config.GetBindable<bool>(OsuSetting.EditorCopyVolumes),
                        },
                        new FormCheckBox
                        {
                            Caption = EditorDialogsStrings.PreserveFivePercentVolume,
                            Current = config.GetBindable<bool>(OsuSetting.EditorAlwaysPreserve5PercentVolume),
                        },
                        new FormCheckBox
                        {
                            Caption = EditorDialogsStrings.MuteSliderends,
                            Current = config.GetBindable<bool>(OsuSetting.EditorMuteRepeatEnds),
                        },
                    }
                });
            }
        }
    }
}
