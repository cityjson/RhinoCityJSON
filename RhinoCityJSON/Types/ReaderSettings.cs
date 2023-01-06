﻿using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace RhinoCityJSON.Types
{
    public class ReaderSettings
    {
        bool translate_ = false;
        Point3d modelOrigin_ = new Point3d(0, 0, 0);
        double trueNorth_ = 0;
        List<string> LoD_ = new List<string>();

        public ReaderSettings() { }

        public ReaderSettings(ReaderSettings other)
        {
            translate_ = other.translate_;
            modelOrigin_ = other.modelOrigin_;
            trueNorth_ = other.trueNorth_;
            LoD_ = other.LoD_;
        }
        public ReaderSettings
        (
            bool translate,
            Point3d modelOrigin,
            double trueNorth,
            List<string> LoDList
            )
        {
            translate_ = translate;
            modelOrigin_ = modelOrigin;
            trueNorth_ = trueNorth;
            LoD_ = LoDList;
        }

        public bool getTranslate() { return translate_; }
        public Point3d getModelOrigin() { return modelOrigin_; }
        public double getTrueNorth() { return trueNorth_; }
        public List<string> getLoDList() { return LoD_; }

        public bool isValid()
        {
            return true;
        }

    }

    public class GHReaderSettings : GH_Goo<ReaderSettings>
    {
        public GHReaderSettings(ReaderSettings readerSettings)
        {
            this.Value = new ReaderSettings(
                new ReaderSettings(readerSettings));
        }

        public GHReaderSettings(GHReaderSettings other)
        {
            this.Value = new ReaderSettings(other.Value);
        }

        public override string TypeName => "CJSettings";

        public override string TypeDescription => "The settings required for the document and template reader object";

        public override bool IsValid => Value.isValid();

        public override IGH_Goo Duplicate() => new GHReaderSettings(this);

        public override string ToString() => "CityJSON Reader Settings";
    }

}