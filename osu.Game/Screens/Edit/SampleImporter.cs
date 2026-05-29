// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit
{
    public partial class SampleImporter
    {
        /// <summary>
        /// Import sample data from playable beatmap
        /// </summary>
        /// <param name="originalBeatmap">The <see cref="EditorBeatmap"/> to export samples to</param>
        /// <param name="sourceBeatmap">The <see cref="IBeatmap"/> to export samples from</param>
        /// <returns>The modified <see cref="IReadOnlyList{HitObject}"/></returns>
        public static void ImportFromBeatmap(EditorBeatmap originalBeatmap, IBeatmap sourceBeatmap)
        {
            CheckHitObjectCounts(originalBeatmap.HitObjects, sourceBeatmap.HitObjects);

            originalBeatmap.BeginChange();

            var sourceData = getHitSampleDataFromBeatmap(sourceBeatmap);

            foreach (var originalHitObject in originalBeatmap.HitObjects)
            {
                assignHitSamplesToObject(originalHitObject, sourceData);
            }

            originalBeatmap.UpdateAllHitObjects();
            originalBeatmap.EndChange();
        }

        private static Dictionary<double, IList<HitSampleInfo>> getHitSampleDataFromBeatmap(IBeatmap beatmap)
        {
            var result = new Dictionary<double, IList<HitSampleInfo>>();

            foreach (var hitObject in beatmap.HitObjects)
            {
                double time = hitObject.StartTime;

                if (hitObject is IHasRepeats hasRepeats)
                {
                    int spanCount = hasRepeats.SpanCount();

                    for (int i = 0; i < spanCount + 1; i++)
                    {
                        time = Math.Round(hitObject.StartTime + hasRepeats.Duration / spanCount * i);
                        result[time] = hasRepeats.NodeSamples[i];
                    }
                }
                else result[time] = hitObject.Samples;
            }

            return result;
        }

        private static void assignHitSamplesToObject(HitObject hitObject, Dictionary<double, IList<HitSampleInfo>> data)
        {
            double time = hitObject.StartTime;
            double? appropriateKey = data.Keys.FirstOrDefault(k => k >= time - 1 && k <= time + 1);

            if (hitObject is IHasRepeats hasRepeats)
            {
                int spanCount = hasRepeats.SpanCount();

                for (int i = 0; i < spanCount + 1; i++)
                {
                    time = Math.Round(hitObject.StartTime + hasRepeats.Duration / spanCount * i);
                    appropriateKey = data.Keys.FirstOrDefault(k => k >= time - 1 && k <= time + 1);

                    if (appropriateKey != 0)
                    {
                        hasRepeats.NodeSamples[i] = data[(double)appropriateKey];
                    }
                }
            }
            else if (appropriateKey != 0)
            {
                hitObject.Samples = data[(double)appropriateKey];
            }
        }

        /// <summary>
        /// Check the original and exporting <see cref="IReadOnlyList{HitObject}"/> counts
        /// </summary>
        /// <param name="originalHitObjects">The <see cref="IReadOnlyList{HitObject}"/> that the <see cref="SampleInfo"/>s are being imported into</param>
        /// <param name="exportedHitObjects">The <see cref="IReadOnlyList{HitObject}"/> that the <see cref="SampleInfo"/>s are being exported from</param>
        /// <exception cref="ArgumentException">If either the original <see cref="IReadOnlyList{HitObject}"/>
        /// or the exporting <see cref="IReadOnlyList{HitObject}"/> has no hit objects</exception>
        protected static void CheckHitObjectCounts(IReadOnlyList<HitObject> originalHitObjects, IReadOnlyList<HitObject> exportedHitObjects)
        {
            if (originalHitObjects.Count == 0) throw new ArgumentException("There are no hit objects to import samples to");
            if (exportedHitObjects.Count == 0) throw new ArgumentException("There are no hit objects to export samples from");
        }
    }
}
