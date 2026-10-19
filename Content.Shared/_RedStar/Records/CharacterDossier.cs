using System.Linq;
using Robust.Shared.Serialization;
using Content.Shared.StationRecords;

namespace Content.Shared._RedStar.Records;

/// <summary>
/// Player-authored RP information persisted with a character profile.
/// A copy is attached to the native station record for the duration of a round.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial record CharacterDossier : StationRecord
{
    public const int ShortTextLength = 128;
    public const int LongTextLength = 2048;
    public const int MaxEntriesPerSection = 16;
    public const int MaxWeight = 300;
    public const int MaxHeight = 300;

    [DataField] public int Height { get; private set; } = 180;
    [DataField] public int Weight { get; private set; } = 70;
    [DataField] public string EmergencyContact { get; private set; } = string.Empty;
    [DataField] public string Residence { get; private set; } = string.Empty;
    [DataField] public string MaritalStatus { get; private set; } = string.Empty;
    [DataField] public string IdentifyingFeatures { get; private set; } = string.Empty;
    [DataField] public string GeneralNotes { get; private set; } = string.Empty;

    [DataField] public string Education { get; private set; } = string.Empty;
    [DataField] public string PreviousEmployment { get; private set; } = string.Empty;
    [DataField] public string ProfessionalExperience { get; private set; } = string.Empty;
    [DataField] public string CertificationsAndTraining { get; private set; } = string.Empty;
    [DataField] public string EmploymentNotes { get; private set; } = string.Empty;

    [DataField] public string Allergies { get; private set; } = string.Empty;
    [DataField] public string DrugAllergies { get; private set; } = string.Empty;
    [DataField] public string MedicalConditions { get; private set; } = string.Empty;
    [DataField] public string Prescriptions { get; private set; } = string.Empty;
    [DataField] public string PostmortemInstructions { get; private set; } = string.Empty;
    [DataField] public string Prosthetics { get; private set; } = string.Empty;
    [DataField] public string PsychologicalNotes { get; private set; } = string.Empty;

    [DataField] public string SecurityBackground { get; private set; } = string.Empty;
    [DataField] public string KnownAssociates { get; private set; } = string.Empty;
    [DataField] public string ThreatAssessment { get; private set; } = string.Empty;
    [DataField] public string SecurityNotes { get; private set; } = string.Empty;

    [DataField] public List<DossierEntry> EmploymentEntries { get; private set; } = [];
    [DataField] public List<DossierEntry> MedicalEntries { get; private set; } = [];
    [DataField] public List<DossierEntry> SecurityEntries { get; private set; } = [];

    public CharacterDossier() { }

    private CharacterDossier(CharacterDossier other) : base(other)
    {
        Height = other.Height;
        Weight = other.Weight;
        EmergencyContact = other.EmergencyContact;
        Residence = other.Residence;
        MaritalStatus = other.MaritalStatus;
        IdentifyingFeatures = other.IdentifyingFeatures;
        GeneralNotes = other.GeneralNotes;
        Education = other.Education;
        PreviousEmployment = other.PreviousEmployment;
        ProfessionalExperience = other.ProfessionalExperience;
        CertificationsAndTraining = other.CertificationsAndTraining;
        EmploymentNotes = other.EmploymentNotes;
        Allergies = other.Allergies;
        DrugAllergies = other.DrugAllergies;
        MedicalConditions = other.MedicalConditions;
        Prescriptions = other.Prescriptions;
        PostmortemInstructions = other.PostmortemInstructions;
        Prosthetics = other.Prosthetics;
        PsychologicalNotes = other.PsychologicalNotes;
        SecurityBackground = other.SecurityBackground;
        KnownAssociates = other.KnownAssociates;
        ThreatAssessment = other.ThreatAssessment;
        SecurityNotes = other.SecurityNotes;
        EmploymentEntries = other.EmploymentEntries.Select(x => new DossierEntry(x)).ToList();
        MedicalEntries = other.MedicalEntries.Select(x => new DossierEntry(x)).ToList();
        SecurityEntries = other.SecurityEntries.Select(x => new DossierEntry(x)).ToList();
    }

    public CharacterDossier DeepCopy() => new(this);

    public void EnsureValid()
    {
        Height = Math.Clamp(Height, 1, MaxHeight);
        Weight = Math.Clamp(Weight, 1, MaxWeight);
        EmergencyContact = Clamp(EmergencyContact, ShortTextLength);
        Residence = Clamp(Residence, ShortTextLength);
        MaritalStatus = Clamp(MaritalStatus, ShortTextLength);
        IdentifyingFeatures = Clamp(IdentifyingFeatures, LongTextLength);
        GeneralNotes = Clamp(GeneralNotes, LongTextLength);
        Education = Clamp(Education, LongTextLength);
        PreviousEmployment = Clamp(PreviousEmployment, LongTextLength);
        ProfessionalExperience = Clamp(ProfessionalExperience, LongTextLength);
        CertificationsAndTraining = Clamp(CertificationsAndTraining, LongTextLength);
        EmploymentNotes = Clamp(EmploymentNotes, LongTextLength);
        Allergies = Clamp(Allergies, LongTextLength);
        DrugAllergies = Clamp(DrugAllergies, LongTextLength);
        MedicalConditions = Clamp(MedicalConditions, LongTextLength);
        Prescriptions = Clamp(Prescriptions, LongTextLength);
        PostmortemInstructions = Clamp(PostmortemInstructions, LongTextLength);
        Prosthetics = Clamp(Prosthetics, LongTextLength);
        PsychologicalNotes = Clamp(PsychologicalNotes, LongTextLength);
        SecurityBackground = Clamp(SecurityBackground, LongTextLength);
        KnownAssociates = Clamp(KnownAssociates, LongTextLength);
        ThreatAssessment = Clamp(ThreatAssessment, LongTextLength);
        SecurityNotes = Clamp(SecurityNotes, LongTextLength);
        ValidateEntries(EmploymentEntries);
        ValidateEntries(MedicalEntries);
        ValidateEntries(SecurityEntries);
    }

    public bool MemberwiseEquals(CharacterDossier other) =>
        Height == other.Height && Weight == other.Weight &&
        EmergencyContact == other.EmergencyContact && Residence == other.Residence &&
        MaritalStatus == other.MaritalStatus &&
        IdentifyingFeatures == other.IdentifyingFeatures && GeneralNotes == other.GeneralNotes &&
        Education == other.Education && PreviousEmployment == other.PreviousEmployment &&
        ProfessionalExperience == other.ProfessionalExperience &&
        CertificationsAndTraining == other.CertificationsAndTraining && EmploymentNotes == other.EmploymentNotes &&
        Allergies == other.Allergies && DrugAllergies == other.DrugAllergies &&
        MedicalConditions == other.MedicalConditions && Prescriptions == other.Prescriptions &&
        PostmortemInstructions == other.PostmortemInstructions && Prosthetics == other.Prosthetics &&
        PsychologicalNotes == other.PsychologicalNotes && SecurityBackground == other.SecurityBackground &&
        KnownAssociates == other.KnownAssociates && ThreatAssessment == other.ThreatAssessment &&
        SecurityNotes == other.SecurityNotes && EntriesEqual(EmploymentEntries, other.EmploymentEntries) &&
        EntriesEqual(MedicalEntries, other.MedicalEntries) && EntriesEqual(SecurityEntries, other.SecurityEntries);

    public CharacterDossier WithHeight(int value) => new(this) { Height = value };
    public CharacterDossier WithWeight(int value) => new(this) { Weight = value };
    public CharacterDossier WithEmergencyContact(string value) => new(this) { EmergencyContact = value };
    public CharacterDossier WithResidence(string value) => new(this) { Residence = value };
    public CharacterDossier WithMaritalStatus(string value) => new(this) { MaritalStatus = value };
    public CharacterDossier WithIdentifyingFeatures(string value) => new(this) { IdentifyingFeatures = value };
    public CharacterDossier WithGeneralNotes(string value) => new(this) { GeneralNotes = value };
    public CharacterDossier WithEducation(string value) => new(this) { Education = value };
    public CharacterDossier WithPreviousEmployment(string value) => new(this) { PreviousEmployment = value };
    public CharacterDossier WithProfessionalExperience(string value) => new(this) { ProfessionalExperience = value };
    public CharacterDossier WithCertificationsAndTraining(string value) => new(this) { CertificationsAndTraining = value };
    public CharacterDossier WithEmploymentNotes(string value) => new(this) { EmploymentNotes = value };
    public CharacterDossier WithAllergies(string value) => new(this) { Allergies = value };
    public CharacterDossier WithDrugAllergies(string value) => new(this) { DrugAllergies = value };
    public CharacterDossier WithMedicalConditions(string value) => new(this) { MedicalConditions = value };
    public CharacterDossier WithPrescriptions(string value) => new(this) { Prescriptions = value };
    public CharacterDossier WithPostmortemInstructions(string value) => new(this) { PostmortemInstructions = value };
    public CharacterDossier WithProsthetics(string value) => new(this) { Prosthetics = value };
    public CharacterDossier WithPsychologicalNotes(string value) => new(this) { PsychologicalNotes = value };
    public CharacterDossier WithSecurityBackground(string value) => new(this) { SecurityBackground = value };
    public CharacterDossier WithKnownAssociates(string value) => new(this) { KnownAssociates = value };
    public CharacterDossier WithThreatAssessment(string value) => new(this) { ThreatAssessment = value };
    public CharacterDossier WithSecurityNotes(string value) => new(this) { SecurityNotes = value };
    public CharacterDossier WithEmploymentEntries(List<DossierEntry> value) => new(this) { EmploymentEntries = Copy(value) };
    public CharacterDossier WithMedicalEntries(List<DossierEntry> value) => new(this) { MedicalEntries = Copy(value) };
    public CharacterDossier WithSecurityEntries(List<DossierEntry> value) => new(this) { SecurityEntries = Copy(value) };

    public CharacterDossier ForSection(DossierSection section)
    {
        var result = DeepCopy();
        if (section != DossierSection.Employment)
        {
            result.MaritalStatus = string.Empty;
            result.GeneralNotes = string.Empty;
            result.Education = string.Empty;
            result.PreviousEmployment = string.Empty;
            result.ProfessionalExperience = string.Empty;
            result.CertificationsAndTraining = string.Empty;
            result.EmploymentNotes = string.Empty;
            result.EmploymentEntries = [];
        }

        if (section != DossierSection.Medical && section != DossierSection.Security)
        {
            result.Height = 0;
            result.Weight = 0;
        }

        if (section != DossierSection.Medical)
        {
            result.Prosthetics = string.Empty;
            result.PsychologicalNotes = string.Empty;
            result.Allergies = string.Empty;
            result.DrugAllergies = string.Empty;
            result.MedicalConditions = string.Empty;
            result.Prescriptions = string.Empty;
            result.PostmortemInstructions = string.Empty;
            result.MedicalEntries = [];
        }

        if (section != DossierSection.Security)
        {
            result.IdentifyingFeatures = string.Empty;
            result.SecurityBackground = string.Empty;
            result.KnownAssociates = string.Empty;
            result.ThreatAssessment = string.Empty;
            result.SecurityNotes = string.Empty;
            result.SecurityEntries = [];
        }

        if (section == DossierSection.Medical)
            result.Residence = string.Empty;

        return result;
    }

    private static string Clamp(string? value, int length) => string.IsNullOrEmpty(value)
        ? string.Empty
        : value[..Math.Min(value.Length, length)];

    private static List<DossierEntry> Copy(IEnumerable<DossierEntry> entries) =>
        entries.Take(MaxEntriesPerSection).Select(x => new DossierEntry(x)).ToList();

    private static void ValidateEntries(List<DossierEntry> entries)
    {
        if (entries.Count > MaxEntriesPerSection)
            entries.RemoveRange(MaxEntriesPerSection, entries.Count - MaxEntriesPerSection);
        foreach (var entry in entries)
        {
            entry.EnsureValid();
        }
    }

    private static bool EntriesEqual(List<DossierEntry> left, List<DossierEntry> right) =>
        left.Count == right.Count && !left.Where((entry, index) => !entry.MemberwiseEquals(right[index])).Any();
}

[Serializable, NetSerializable]
public enum DossierSection : byte
{
    Employment,
    Medical,
    Security,
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class DossierEntry
{
    [DataField] public string Title { get; private set; } = string.Empty;
    [DataField] public string Involved { get; private set; } = string.Empty;
    [DataField] public string Description { get; private set; } = string.Empty;

    public DossierEntry() { }
    public DossierEntry(string title, string involved, string description) =>
        (Title, Involved, Description) = (title, involved, description);
    public DossierEntry(DossierEntry other) : this(other.Title, other.Involved, other.Description) { }

    public void EnsureValid()
    {
        Title = Clamp(Title, CharacterDossier.ShortTextLength);
        Involved = Clamp(Involved, CharacterDossier.ShortTextLength);
        Description = Clamp(Description, CharacterDossier.LongTextLength);
    }

    public bool MemberwiseEquals(DossierEntry other) =>
        Title == other.Title && Involved == other.Involved && Description == other.Description;

    private static string Clamp(string? value, int length) => string.IsNullOrEmpty(value)
        ? string.Empty
        : value[..Math.Min(value.Length, length)];
}
