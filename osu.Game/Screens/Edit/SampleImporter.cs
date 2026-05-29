// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit
{
    public partial class SampleImporter
    {
        private Bindable<double> leniency = null!;
        private Bindable<SampleCopyMode> copyMode = null!;
        private Bindable<bool> shouldCopySamples = null!;
        private Bindable<bool> shouldCopyBanks = null!;
        private Bindable<bool> shouldCopyVolumes = null!;
        private Bindable<bool> shouldPreserve5PercentVolume = null!;
        private Bindable<bool> shouldMuteSliderends = null!;

        public void Load(OsuConfigManager config)
        {
            leniency = config.GetBindable<double>(OsuSetting.EditorSampleCopyLeniency);
            copyMode = config.GetBindable<SampleCopyMode>(OsuSetting.EditorSampleCopyMode);
            shouldCopySamples = config.GetBindable<bool>(OsuSetting.EditorCopySamples);
            shouldCopyBanks = config.GetBindable<bool>(OsuSetting.EditorCopyBanks);
            shouldCopyVolumes = config.GetBindable<bool>(OsuSetting.EditorCopyVolumes);
            shouldPreserve5PercentVolume = config.GetBindable<bool>(OsuSetting.EditorAlwaysPreserve5PercentVolume);
            shouldMuteSliderends = config.GetBindable<bool>(OsuSetting.EditorMuteRepeatEnds);
        }

        /// <summary>
        /// Import sample data from playable beatmap
        /// </summary>
        /// <param name="originalBeatmap">The <see cref="EditorBeatmap"/> to export samples to</param>
        /// <param name="sourceBeatmap">The <see cref="IBeatmap"/> to export samples from</param>
        /// <returns>The modified <see cref="IReadOnlyList{HitObject}"/></returns>
        public void ImportFromBeatmap(EditorBeatmap originalBeatmap, IBeatmap sourceBeatmap)
        {
            checkHitObjectCounts(originalBeatmap.HitObjects, sourceBeatmap.HitObjects);

            originalBeatmap.BeginChange();

            var sourceData = getHitSampleDataFromBeatmap(sourceBeatmap);

            foreach (var originalHitObject in originalBeatmap.HitObjects)
            {
                assignHitSamplesToObject(originalHitObject, sourceData);
            }

            originalBeatmap.UpdateAllHitObjects();
            originalBeatmap.EndChange();
        }

        private Dictionary<double, IList<HitSampleInfo>> getHitSampleDataFromBeatmap(IBeatmap beatmap)
        {
            var result = new Dictionary<double, IList<HitSampleInfo>>();

            foreach (var hitObject in beatmap.HitObjects)
            {
                double time;

                if (hitObject is IHasRepeats hasRepeats)
                {
                    int spanCount = hasRepeats.SpanCount();

                    for (int i = 0; i < spanCount + 1; i++)
                    {
                        time = Math.Round(hitObject.StartTime + hasRepeats.Duration / spanCount * i);
                        result[time] = hasRepeats.NodeSamples[i];
                    }
                }
                else
                {
                    time = hitObject.StartTime;
                    result[time] = hitObject.Samples;
                }
            }

            return result;
        }

        private void assignHitSamplesToObject(HitObject hitObject, Dictionary<double, IList<HitSampleInfo>> data)
        {
            List<HitSampleInfo> sampleInfos = new List<HitSampleInfo>();
            double time;
            double? appropriateKey;

            if (hitObject is IHasRepeats hasRepeats)
            {
                int spanCount = hasRepeats.SpanCount();

                for (int i = 0; i < spanCount + 1; i++)
                {
                    time = Math.Round(hitObject.StartTime + hasRepeats.Duration / spanCount * i);
                    appropriateKey = data.Keys.FirstOrDefault(k => k >= time - leniency.Value && k <= time + leniency.Value);
                    if (appropriateKey != 0) sampleInfos = data[(double)appropriateKey].ToList();

                    applyChangesToHitSampleInfo(hasRepeats.NodeSamples[i], sampleInfos);

                    // Apply 5% volume if it's the final node and it should be muted
                    if (i == spanCount && shouldMuteSliderends.Value)
                    {
                        hasRepeats.NodeSamples[i] = hasRepeats.NodeSamples[i].Select(info => info.With(newVolume: 5)).ToList();
                    }
                }
            }
            else
            {
                time = hitObject.StartTime;
                appropriateKey = data.Keys.FirstOrDefault(k => k >= time - leniency.Value && k <= time + leniency.Value);
                if (appropriateKey != 0) sampleInfos = data[(double)appropriateKey].ToList();

                applyChangesToHitSampleInfo(hitObject.Samples, sampleInfos);
            }
        }

        /// <summary>
        /// Modify the original <see cref="IList{HitSampleInfo}"/> according to provided source <see cref="IList{HitSampleInfo}"/>
        /// </summary>
        /// <param name="originalSampleInfos">The <see cref="IList{HitSampleInfo}"/> to modify</param>
        /// <param name="sourceSampleInfos">The <see cref="IList{HitSampleInfo}"/> to get data from</param>
        private void applyChangesToHitSampleInfo(IList<HitSampleInfo> originalSampleInfos, IList<HitSampleInfo> sourceSampleInfos)
        {
            // Default data for the "Overwrite everything" copy mode when there's no appropriate sample
            HitSampleInfo defaultData = originalSampleInfos[0].With(newVolume: 100, newBank: "normal", newName: "hitnormal");
            List<HitSampleInfo> defaultSampleInfos = new List<HitSampleInfo> { defaultData };

            int baseVolume = originalSampleInfos[0].Volume; // For when the "Preserve 5% volume" toggle is turned on

            // Only reset the sample data if we overwrite samples for every object.
            if (!sourceSampleInfos.Any() && copyMode.Value == SampleCopyMode.OverwriteAllSamples) originalSampleInfos = defaultSampleInfos;
            else if (sourceSampleInfos.Any())
            {
                originalSampleInfos = sourceSampleInfos.Select(info => info.With(
                    newName: shouldCopySamples.Value ? info.Name : "hitnormal",
                    newBank: shouldCopyBanks.Value ? info.Bank : "normal",
                    newVolume: shouldCopyVolumes.Value ? info.Volume : 100)).ToList();
            }

            if (baseVolume == 5 && shouldPreserve5PercentVolume.Value) originalSampleInfos = originalSampleInfos.Select(info => info.With(newVolume: 5)).ToList();
        }

        /// <summary>
        /// Check the original and exporting <see cref="IReadOnlyList{HitObject}"/> counts
        /// </summary>
        /// <param name="originalHitObjects">The <see cref="IReadOnlyList{HitObject}"/> that the <see cref="SampleInfo"/>s are being imported into</param>
        /// <param name="exportedHitObjects">The <see cref="IReadOnlyList{HitObject}"/> that the <see cref="SampleInfo"/>s are being exported from</param>
        /// <exception cref="ArgumentException">If either the original <see cref="IReadOnlyList{HitObject}"/>
        /// or the exporting <see cref="IReadOnlyList{HitObject}"/> has no hit objects</exception>
        private static void checkHitObjectCounts(IReadOnlyList<HitObject> originalHitObjects, IReadOnlyList<HitObject> exportedHitObjects)
        {
            if (originalHitObjects.Count == 0) throw new ArgumentException("There are no hit objects to import samples to");
            if (exportedHitObjects.Count == 0) throw new ArgumentException("There are no hit objects to export samples from");
        }
    }
}
