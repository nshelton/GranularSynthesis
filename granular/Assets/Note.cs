using System;

public enum Note
{
    C0,
    Cs0,
    Db0,
    D0,
    Ds0,
    Eb0,
    E0,
    F0,
    Fs0,
    Gb0,
    G0,
    Gs0,
    Ab0,
    A0,
    As0,
    Bb0,
    B0,
    C1,
    Cs1,
    Db1,
    D1,
    Ds1,
    Eb1,
    E1,
    F1,
    Fs1,
    Gb1,
    G1,
    Gs1,
    Ab1,
    A1,
    As1,
    Bb1,
    B1,
    C2,
    Cs2,
    Db2,
    D2,
    Ds2,
    Eb2,
    E2,
    F2,
    Fs2,
    Gb2,
    G2,
    Gs2,
    Ab2,
    A2,
    As2,
    Bb2,
    B2,
    C3,
    Cs3,
    Db3,
    D3,
    Ds3,
    Eb3,
    E3,
    F3,
    Fs3,
    Gb3,
    G3,
    Gs3,
    Ab3,
    A3,
    As3,
    Bb3,
    B3,
    C4,
    Cs4,
    Db4,
    D4,
    Ds4,
    Eb4,
    E4,
    F4,
    Fs4,
    Gb4,
    G4,
    Gs4,
    Ab4,
    A4,
    As4,
    Bb4,
    B4,
    C5,
    Cs5,
    Db5,
    D5,
    Ds5,
    Eb5,
    E5,
    F5,
    Fs5,
    Gb5,
    G5,
    Gs5,
    Ab5,
    A5,
    As5,
    Bb5,
    B5,
    C6,
    Cs6,
    Db6,
    D6,
    Ds6,
    Eb6,
    E6,
    F6,
    Fs6,
    Gb6,
    G6,
    Gs6,
    Ab6,
    A6,
    As6,
    Bb6,
    B6,
    C7,
    Cs7,
    Db7,
    D7,
    Ds7,
    Eb7,
    E7,
    F7,
    Fs7,
    Gb7,
    G7,
    Gs7,
    Ab7,
    A7,
    As7,
    Bb7,
    B7,
    C8,
    Cs8,
    Db8,
    D8,
    Ds8,
    Eb8,
    E8,
    F8,
    Fs8,
    Gb8,
    G8,
    Gs8,
    Ab8,
    A8,
    As8,
    Bb8,
    B8,
}

public static class Notes
{
    public static float GetFreq(Note noteName)
    {
        switch (noteName)
        {
            case Note.C0:
                return 16.35f;
            case Note.Cs0:
            case Note.Db0:
                return 17.32f;
            case Note.D0:
                return 18.35f;
            case Note.Ds0:
            case Note.Eb0:
                return 19.45f;
            case Note.E0:
                return 20.60f;
            case Note.F0:
                return 21.83f;
            case Note.Fs0:
            case Note.Gb0:
                return 23.12f;
            case Note.G0:
                return 24.50f;
            case Note.Gs0:
            case Note.Ab0:
                return 25.96f;
            case Note.A0:
                return 27.50f;
            case Note.As0:
            case Note.Bb0:
                return 29.14f;
            case Note.B0:
                return 30.87f;
            case Note.C1:
                return 32.70f;
            case Note.Cs1:
            case Note.Db1:
                return 34.65f;
            case Note.D1:
                return 36.71f;
            case Note.Ds1:
            case Note.Eb1:
                return 38.89f;
            case Note.E1:
                return 41.20f;
            case Note.F1:
                return 43.65f;
            case Note.Fs1:
            case Note.Gb1:
                return 46.25f;
            case Note.G1:
                return 49.00f;
            case Note.Gs1:
            case Note.Ab1:
                return 51.91f;
            case Note.A1:
                return 55.00f;
            case Note.As1:
            case Note.Bb1:
                return 58.27f;
            case Note.B1:
                return 61.74f;
            case Note.C2:
                return 65.41f;
            case Note.Cs2:
            case Note.Db2:
                return 69.30f;
            case Note.D2:
                return 73.42f;
            case Note.Ds2:
            case Note.Eb2:
                return 77.78f;
            case Note.E2:
                return 82.41f;
            case Note.F2:
                return 87.31f;
            case Note.Fs2:
            case Note.Gb2:
                return 92.50f;
            case Note.G2:
                return 98.00f;
            case Note.Gs2:
            case Note.Ab2:
                return 103.83f;
            case Note.A2:
                return 110.00f;
            case Note.As2:
            case Note.Bb2:
                return 116.54f;
            case Note.B2:
                return 123.47f;
            case Note.C3:
                return 130.81f;
            case Note.Cs3:
            case Note.Db3:
                return 138.59f;
            case Note.D3:
                return 146.83f;
            case Note.Ds3:
            case Note.Eb3:
                return 155.56f;
            case Note.E3:
                return 164.81f;
            case Note.F3:
                return 174.61f;
            case Note.Fs3:
            case Note.Gb3:
                return 185.00f;
            case Note.G3:
                return 196.00f;
            case Note.Gs3:
            case Note.Ab3:
                return 207.65f;
            case Note.A3:
                return 220.00f;
            case Note.As3:
            case Note.Bb3:
                return 233.08f;
            case Note.B3:
                return 246.94f;
            case Note.C4:
                return 261.63f;
            case Note.Cs4:
            case Note.Db4:
                return 277.18f;
            case Note.D4:
                return 293.66f;
            case Note.Ds4:
            case Note.Eb4:
                return 311.13f;
            case Note.E4:
                return 329.63f;
            case Note.F4:
                return 349.23f;
            case Note.Fs4:
            case Note.Gb4:
                return 369.99f;
            case Note.G4:
                return 392.00f;
            case Note.Gs4:
            case Note.Ab4:
                return 415.30f;
            case Note.A4:
                return 440.00f;
            case Note.As4:
            case Note.Bb4:
                return 466.16f;
            case Note.B4:
                return 493.88f;
            case Note.C5:
                return 523.25f;
            case Note.Cs5:
            case Note.Db5:
                return 554.37f;
            case Note.D5:
                return 587.33f;
            case Note.Ds5:
            case Note.Eb5:
                return 622.25f;
            case Note.E5:
                return 659.25f;
            case Note.F5:
                return 698.46f;
            case Note.Fs5:
            case Note.Gb5:
                return 739.99f;
            case Note.G5:
                return 783.99f;
            case Note.Gs5:
            case Note.Ab5:
                return 830.61f;
            case Note.A5:
                return 880.00f;
            case Note.As5:
            case Note.Bb5:
                return 932.33f;
            case Note.B5:
                return 987.77f;
            case Note.C6:
                return 1046.50f;
            case Note.Cs6:
            case Note.Db6:
                return 1108.73f;
            case Note.D6:
                return 1174.66f;
            case Note.Ds6:
            case Note.Eb6:
                return 1244.51f;
            case Note.E6:
                return 1318.51f;
            case Note.F6:
                return 1396.91f;
            case Note.Fs6:
            case Note.Gb6:
                return 1479.98f;
            case Note.G6:
                return 1567.98f;
            case Note.Gs6:
            case Note.Ab6:
                return 1661.22f;
            case Note.A6:
                return 1760.00f;
            case Note.As6:
            case Note.Bb6:
                return 1864.66f;
            case Note.B6:
                return 1975.53f;
            case Note.C7:
                return 2093.00f;
            case Note.Cs7:
            case Note.Db7:
                return 2217.46f;
            case Note.D7:
                return 2349.32f;
            case Note.Ds7:
            case Note.Eb7:
                return 2489.02f;
            case Note.E7:
                return 2637.02f;
            case Note.F7:
                return 2793.83f;
            case Note.Fs7:
            case Note.Gb7:
                return 2959.96f;
            case Note.G7:
                return 3135.96f;
            case Note.Gs7:
            case Note.Ab7:
                return 3322.44f;
            case Note.A7:
                return 3520.00f;
            case Note.As7:
            case Note.Bb7:
                return 3729.31f;
            case Note.B7:
                return 3951.07f;
            case Note.C8:
                return 4186.01f;
            case Note.Cs8:
            case Note.Db8:
                return 4434.92f;
            case Note.D8:
                return 4698.63f;
            case Note.Ds8:
            case Note.Eb8:
                return 4978.03f;
            case Note.E8:
                return 5274.04f;
            case Note.F8:
                return 5587.65f;
            case Note.Fs8:
            case Note.Gb8:
                return 5919.91f;
            case Note.G8:
                return 6271.93f;
            case Note.Gs8:
            case Note.Ab8:
                return 6644.88f;
            case Note.A8:
                return 7040.00f;
            case Note.As8:
            case Note.Bb8:
                return 7458.62f;
            case Note.B8:
                return 7902.13f;
            default:
                return 0f;
        }
    }


}
