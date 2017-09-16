﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace _7thSagaRando
{
    public partial class Form1 : Form
    {
        byte[] monsterRanking = { 0x24, 0x2f, 0x02, 0x29, 0x30, 0x08, 0x1a, 0x11,
                                      0x0a, 0x40, 0x05, 0x03, 0x0d, 0x07, 0x2c, 0x04,
                                      0x1e, 0x2d, 0x1b, 0x09, 0x3d, 0x43, 0x41, 0x50,
                                      0x15, 0x19, 0x2a, 0x21, 0x1c, 0x06, 0x4e, 0x55,
                                      0x51, 0x0e, 0x2e, 0x0f, 0x16, 0x44, 0x1d, 0x49,
                                      0x3e, 0x45, 0x31, 0x4c, 0x56, 0x57, 0x54, 0x46,
                                      0x17, 0x4d, 0x42, 0x52, 0x4f, 0x47, 0x18, 0x53,
                                      0x48, 0x32, 0x3f, 0x1f, 0x20, 0x23, 0x27, 0x58,
                                      0x4a, 0x59 }; // 66 monsters total
        byte[] legalSpells = { 1, 2, 3, 4, 5, 6, 7,
                                12, 13, 14, 15,
                                16, 17, 21, 22, 23,
                                24, 25, 26, 27, 28, 29, 30, 31,
                                33, 34, 35,
                                40, 41, 45, 46, 47 };

        bool loading = true;
        byte[] romData;
        byte[] romData2;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnNewSeed_Click(object sender, EventArgs e)
        {
            txtSeed.Text = (DateTime.Now.Ticks % 2147483647).ToString();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFileName.Text = openFileDialog1.FileName;
                runChecksum();
            }
        }

        private void runChecksum()
        {
            try
            {
                using (var md5 = SHA1.Create())
                {
                    using (var stream = File.OpenRead(txtFileName.Text))
                    {
                        lblSHAChecksum.Text = BitConverter.ToString(md5.ComputeHash(stream)).ToLower().Replace("-", "");
                    }
                }
            }
            catch
            {
                lblSHAChecksum.Text = "????????????????????????????????????????";
            }
        }

        private bool loadRom(bool extra = false)
        {
            try
            {
                romData = File.ReadAllBytes(txtFileName.Text);
                if (extra)
                    romData2 = File.ReadAllBytes(txtCompare.Text);
            }
            catch
            {
                MessageBox.Show("Empty file name(s) or unable to open files.  Please verify the files exist.");
                return false;
            }
            return true;
        }

        private void saveRom()
        {
            string options = "";
            string finalFile = Path.Combine(Path.GetDirectoryName(txtFileName.Text), "DQ12R_" + txtSeed.Text + "_" + txtFlags.Text + ".smc");
            File.WriteAllBytes(finalFile, romData);
            lblStatus.Text = "ROM hacking complete!  (" + finalFile + ")";
            txtCompare.Text = finalFile;
        }

        private void swap(int firstAddress, int secondAddress)
        {
            byte holdAddress = romData[secondAddress];
            romData[secondAddress] = romData[firstAddress];
            romData[firstAddress] = holdAddress;
        }

        private int[] swapArray(int[] array, int first, int second)
        {
            int holdAddress = array[second];
            array[second] = array[first];
            array[first] = holdAddress;
            return array;
        }

        private int ScaleValue(int value, double scale, double adjustment, Random r1)
        {
            var exponent = (double)r1.Next() / int.MaxValue * 2.0 - 1.0;
            var adjustedScale = 1.0 + adjustment * (scale - 1.0);

            return (int)Math.Round(Math.Pow(adjustedScale, exponent) * value, MidpointRounding.AwayFromZero);
        }

        private int[] inverted_power_curve(int min, int max, int arraySize, double powToUse, Random r1)
        {
            int range = max - min;
            double p_range = Math.Pow(range, 1 / powToUse);
            int[] points = new int[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                double section = (double)r1.Next() / int.MaxValue;
                points[i] = (int)Math.Round(max - Math.Pow(section * p_range, powToUse));
            }
            Array.Sort(points);
            return points;
        }

        private void determineFlags(object sender, EventArgs e)
        {
            if (loading)
                return;

            string flags = "";
            int number = (chkMonsterZones.Checked ? 1 : 0) + (chkMonsterPatterns.Checked ? 2 : 0) + (chkHeroStats.Checked ? 4 : 0) +
                (chkTreasures.Checked ? 8 : 0) + (chkStores.Checked ? 16 : 0) + (chkWhoCanEquip.Checked ? 32 : 0);
            flags += convertIntToChar(number);
            flags += convertIntToChar(trkExperience.Value);
            flags += convertIntToChar(trkGoldReq.Value);

            txtFlags.Text = flags;
        }

        private void determineChecks(object sender, EventArgs e)
        {
            string flags = txtFlags.Text;
            int number = convertChartoInt(Convert.ToChar(flags.Substring(0, 1)));
            chkMonsterZones.Checked = (number % 2 == 1);
            chkMonsterPatterns.Checked = (number % 4 >= 2);
            chkHeroStats.Checked = (number % 8 >= 4);
            chkTreasures.Checked = (number % 16 >= 8);
            chkStores.Checked = (number % 32 >= 16);
            chkWhoCanEquip.Checked = (number % 64 >= 32);

            trkExperience.Value = convertChartoInt(Convert.ToChar(flags.Substring(1, 1)));
            trkExperience_Scroll(null, null);
            trkGoldReq.Value = convertChartoInt(Convert.ToChar(flags.Substring(2, 1)));
            trkGoldReq_Scroll(null, null);
        }

        private string convertIntToChar(int number)
        {
            if (number >= 0 && number <= 9)
                return number.ToString();
            if (number >= 10 && number <= 35)
                return Convert.ToChar(55 + number).ToString();
            if (number >= 36 && number <= 61)
                return Convert.ToChar(61 + number).ToString();
            if (number == 62) return "!";
            if (number == 63) return "@";
            return "";
        }

        private int convertChartoInt(char character)
        {
            if (character >= Convert.ToChar("0") && character <= Convert.ToChar("9"))
                return character - 48;
            if (character >= Convert.ToChar("A") && character <= Convert.ToChar("Z"))
                return character - 55;
            if (character >= Convert.ToChar("a") && character <= Convert.ToChar("z"))
                return character - 61;
            if (character == Convert.ToChar("!")) return 62;
            if (character == Convert.ToChar("@")) return 63;
            return 0;
        }

        private void trkExperience_Scroll(object sender, EventArgs e)
        {
            lblExperience.Text = (trkExperience.Value * 20).ToString() + "%";
            determineFlags(null, null);
        }

        private void trkGoldReq_Scroll(object sender, EventArgs e)
        {
            lblGoldReq.Text = (trkGoldReq.Value == 10 ? "100%" : (1000 / trkGoldReq.Value) + "-" + (trkGoldReq.Value * 10).ToString() + "%");
            determineFlags(null, null);
        }

        private void cmdRandomize_Click(object sender, EventArgs e)
        {
            //try
            {
                loadRom();
                Random r1 = new Random(Convert.ToInt32(txtSeed.Text));
                apprenticeFightAdjustment();
                if (chkMonsterZones.Checked) randomizeMonsterZones(r1);
                if (chkMonsterPatterns.Checked) randomizeMonsterPatterns(r1);
                if (chkHeroStats.Checked) randomizeHeroStats(r1);
                if (chkTreasures.Checked) randomizeTreasures(r1);
                if (chkStores.Checked) randomizeStores(r1);
                if (chkWhoCanEquip.Checked) randomizeWhoCanEquip(r1);
                boostExp();
                goldRequirements(r1);
                saveRom();
            }
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error:  " + ex.Message);
            //}
        }

        private void apprenticeFightAdjustment()
        {
            romData[0xbd61] = 0x7f; // Prevent stat boosts when hero level > 10
            romData[0xca59] = 0xe9; // Force apprentice to be your level MINUS 1 instead of your level PLUS 1.
            romData[0x24852] = 0xea; // None of that doubling of XP either.  Sorry, that's cheating.
        }

        private void randomizeMonsterZones(Random r1)
        {
            byte[] bosses = { 0x0d, 0x0e, 0x1e, 0x21, 0x0f, 0x1f, 0x20, 0x23, 0x27, 0x4a, 0x59 };
            byte[] monsterZoneRanking = { 0x01, 0x02, 0x19, 0x1a, 0x1e, 0x1f, 0x20, 0x21,
                                          0x03, 0x00, 0x04, 0x05, 0x22, 0x23, 0x24, 0x06,
                                          0x07, 0x08, 0x09, 0x25, 0x26, 0x0a, 0x27, 0x28,
                                          0x29, 0x2a, 0x30, 0x31, 0x0b, 0x0c, 0x0d, 0x0e,
                                          0x0f, 0x10, 0x11, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
                                          0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x12, 0x13,
                                          0x14, 0x38, 0x39, 0x46, 0x4d, 0x4e, 0x4f, 0x50,
                                          0x58, 0x59, 0x5a }; // 67 zones total

            // Start at 10% chance of help, then increase to a maximum of 60%.
            for (byte lnI = 0; lnI < monsterZoneRanking.Length; lnI++)
            {
                int byteToUse = 0x58df + (monsterZoneRanking[lnI] * 24);
                for (byte lnJ = 0; lnJ < 8; lnJ++)
                {
                    byte minMonster = (byte)(lnI < 46 ? 0 : 10 + ((lnI - 46) * 2));
                    byte maxMonster = (byte)(lnI < 6 ? 5 : lnI < 11 ? 10 : lnI < 18 ? 17 : lnI < 25 ? 26 : 66);
                    byte helpChance = (byte)Math.Min(70, 10 + (lnI * 5));

                    byte monster1 = (byte)(r1.Next() % (maxMonster - minMonster) + minMonster);
                    byte monster2 = 255;
                    byte monster3 = 255;
                    if (r1.Next() % 100 < helpChance && !bosses.Contains(monsterRanking[monster1]))
                    {
                        bool legal = false;
                        while (!legal)
                        {
                            monster2 = (byte)(r1.Next() % (maxMonster - minMonster) + minMonster);
                            if (!bosses.Contains(monsterRanking[monster2])) legal = true;
                        }
                        if (r1.Next() % 100 < helpChance)
                        {
                            legal = false;
                            while (!legal)
                            {
                                monster3 = (byte)(r1.Next() % (maxMonster - minMonster) + minMonster);
                                if (!bosses.Contains(monsterRanking[monster3])) legal = true;
                            }
                        }
                    }
                    romData[byteToUse + lnJ] = monsterRanking[monster1];
                    if (monster2 != 255)
                        romData[byteToUse + lnJ + 8] = monsterRanking[monster2];
                    if (monster3 != 255)
                        romData[byteToUse + lnJ + 16] = monsterRanking[monster3];
                }
            }
        }

        private void randomizeMonsterPatterns(Random r1)
        {
            for (int lnI = 0; lnI < monsterRanking.Length; lnI++)
            {
                int byteToUse = 0x72f4 + (monsterRanking[lnI] * 42);
                for (int lnJ = 0; lnJ < 16; lnJ++)
                    romData[byteToUse + lnJ + 11] = 0x00;
                if (r1.Next() % 2 == 0)
                {
                    int spellTotal = 100;
                    bool duplicate = false;
                    for (int lnJ = 0; lnJ < 8; lnJ++)
                    {
                        romData[byteToUse + lnJ + 11] = legalSpells[lnJ];
                        for (int lnK = 0; lnK < lnJ; lnK++)
                            if (romData[byteToUse + lnJ + 11] == romData[byteToUse + lnK + 11]) { romData[byteToUse + lnJ + 11] = 0; duplicate = true; break; }
                        if (duplicate) break;
                        romData[byteToUse + lnJ + 19] = (byte)(r1.Next() % Math.Min(50, spellTotal) + 1);
                        spellTotal -= romData[byteToUse + lnJ + 19];
                        if (spellTotal <= 0) break;
                    }

                    int mp = romData[byteToUse + 1] + (romData[byteToUse + 2] * 256);
                    if (mp < 8) {
                        mp = r1.Next() % 32;
                        romData[byteToUse + 1] = (byte)mp;
                    }
                }
            }

        }

        private void randomizeHeroStats(Random r1)
        {
            // Output all of this to a text file so players can figure out combinations and the like.
            using (StreamWriter writer = File.CreateText(Path.Combine(Path.GetDirectoryName(txtFileName.Text), "7thSaga_" + txtSeed.Text + "_HeroGuide.txt")))
            {
                for (int lnI = 0; lnI < 7; lnI++)
                {
                    writer.WriteLine(lnI == 0 ? "Kamil" : lnI == 1 ? "Olvan" : lnI == 2 ? "Esuna" : lnI == 3 ? "Wilme" : lnI == 4 ? "Lux" : lnI == 5 ? "Valsu" : "Lejes");
                    int byteToUse = 0x623f + (18 * lnI);
                    romData[byteToUse] = (byte)(r1.Next() % 16 + 12); // Starting HP
                    romData[byteToUse + 2] = (byte)(r1.Next() % 21 + 0); // Starting MP
                    romData[byteToUse + 4] = (byte)(r1.Next() % (lnI >= 2 ? 8 : 7) + (lnI == 3 || lnI == 4 ? 3 : 2)); // Starting Power
                    romData[byteToUse + 5] = (byte)(r1.Next() % (lnI >= 2 ? 8 : 7) + (lnI == 3 || lnI == 4 ? 3 : 2)); // Starting Guard
                    romData[byteToUse + 6] = (byte)(r1.Next() % 7 + 3); // Starting Magic
                    romData[byteToUse + 7] = (byte)(r1.Next() % 7 + 3); // Starting Speed
                    romData[byteToUse + 8] = (byte)(r1.Next() % 7 + 4); // HP Boost
                    romData[byteToUse + 9] = (byte)(r1.Next() % 6 + 2); // MP Boost
                    romData[byteToUse + 10] = (byte)(r1.Next() % (lnI >= 2 ? 6 : 5) + (lnI == 3 || lnI == 4 ? 3 : 2)); // Power Boost
                    romData[byteToUse + 11] = (byte)(r1.Next() % (lnI >= 2 ? 6 : 5) + (lnI == 3 || lnI == 4 ? 3 : 2)); // Guard Boost
                    romData[byteToUse + 12] = (byte)(r1.Next() % 3 + 3); // Magic Boost
                    romData[byteToUse + 13] = (byte)(r1.Next() % 3 + 3); // Speed Boost
                                                                         // 14-16 - Weapon/Armor/Shield
                    romData[byteToUse + 17] = (byte)(r1.Next() % 100 + 0); // Starting Experience

                    writer.WriteLine("Start:   HP:  " + romData[byteToUse] + "  MP:  " + romData[byteToUse + 2] + "  PWR:  " + romData[byteToUse + 4] + "  GRD:  " + romData[byteToUse + 5] + "  MAG:  " + romData[byteToUse + 6] + "  SPD:  " + romData[byteToUse + 7]);
                    writer.WriteLine("Growth:  HP:  " + romData[byteToUse + 8] + "  MP:  " + romData[byteToUse + 9] + "  PWR:  " + romData[byteToUse + 10] + "  GRD:  " + romData[byteToUse + 11] + "  MAG:  " + romData[byteToUse + 12] + "  SPD:  " + romData[byteToUse + 13]);

                    List<byte> actualSpells = new List<byte>();
                    // Learn spells as long as you don't duplicate another spell.
                    for (int lnJ = 0; lnJ < 16; lnJ++)
                    {
                        actualSpells.Add(legalSpells[r1.Next() % legalSpells.Length]);
                        bool duplicate = false;
                        for (int lnK = 0; lnK < lnJ; lnK++)
                            if (actualSpells[lnJ] == actualSpells[lnK]) { duplicate = true; actualSpells.RemoveAt(actualSpells.Count - 1); break; }
                        if (duplicate) break;
                    }

                    int[] spellLevels = inverted_power_curve(1, 45, actualSpells.Count, 1, r1);

                    writer.WriteLine("Spells:");
                    byteToUse = 0x62bd + (32 * lnI);
                    for (int lnJ = 0; lnJ < 32; lnJ++)
                        romData[byteToUse + lnJ] = 0;
                    for (int lnJ = 0; lnJ < actualSpells.Count; lnJ++)
                    {
                        romData[byteToUse + lnJ] = actualSpells[lnJ];
                        romData[byteToUse + lnJ + 16] = (byte)spellLevels[lnJ];

                        writer.WriteLine(spellLevels[lnJ].ToString() + " - " + (actualSpells[lnJ] == 1 ? "FIRE 1" : actualSpells[lnJ] == 2 ? "FIRE 2" : actualSpells[lnJ] == 3 ? "ICE 1" : actualSpells[lnJ] == 4 ? "ICE 2" : actualSpells[lnJ] == 5 ? "LASER 1" : actualSpells[lnJ] == 6 ? "LASER 2" : actualSpells[lnJ] == 7 ? "LASER 3" : actualSpells[lnJ] == 12 ? "F BIRD" : actualSpells[lnJ] == 13 ? "F BALL" : 
                            actualSpells[lnJ] == 14 ? "BLZRD 1" : actualSpells[lnJ] == 15 ? "BLZRD 2" : actualSpells[lnJ] == 16 ? "THNDER1" : actualSpells[lnJ] == 17 ? "THNDER2" : actualSpells[lnJ] == 21 ? "PETRIFY" : actualSpells[lnJ] == 22 ? "DEFNSE1" : actualSpells[lnJ] == 23 ? "DEFNSE2" : actualSpells[lnJ] == 24 ? "HEAL 1" : actualSpells[lnJ] == 25 ? "HEAL 2" : actualSpells[lnJ] == 26 ? "HEAL 3" : 
                            actualSpells[lnJ] == 27 ? "MPCTCHR" : actualSpells[lnJ] == 28 ? "AGILITY" : actualSpells[lnJ] == 29 ? "F SHID" : actualSpells[lnJ] == 30 ? "PROTECT" : actualSpells[lnJ] == 31 ? "EXIT" : actualSpells[lnJ] == 33 ? "POWER" : actualSpells[lnJ] == 34 ? "HPCTCHR" : actualSpells[lnJ] == 35 ? "ELIXIR" : actualSpells[lnJ] == 40 ? "VACUUM1" : actualSpells[lnJ] == 41 ? "VACUUM2" : 
                            actualSpells[lnJ] == 45 ? "PURIFY" : actualSpells[lnJ] == 46 ? "REVIVE1" : "REVIVE2"));
                    }

                    writer.WriteLine("");
                }
            }
        }

        private void randomizeTreasures(Random r1)
        {
        }

        private void randomizeStores(Random r1)
        {
            byte[] weapons = {
                0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c,
                0x6d, 0x6e, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74,
                0x77, 0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e,
                0x7f, 0x80, 0x81, 0x82, 0x83, 0x85, 0x86, 0x87,
                0x88, 0x89, 0x8a, 0x8b, 0x8d, 0x8e, 0x8f, 0x90,
                0x91, 0x92, 0x93, 0x94, 0x95, 0x96
            };
            byte[] armor = {
                0x97, 0x98, 0x99, 0x9a, 0x9b, 0x9c, 0x9d, 0x9e,
                0x9f, 0xa0, 0xa1, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6,
                0xa7, 0xa8, 0xa9, 0xaa, 0xab, 0xac, 0xad, 0xae,
                0xaf, 0xb0, 0xb1, 0xb2, 0xb3, 0xb5, 0xb6, 0xb7,
                0xb8, 0xb9, 0xba, 0xbb, 0xbc, 0xbd, 0xbe, 0xbf,
                0xc0, 0xc1, 0xc2, 0xc7, 0xc8, 0xc9, 0xca, 0xcb
            };

            byte[] items = {
                0x01, 0x02, 0x0b, 0x0c, 0x0d, 0x11, 0x12, 0x13,
                0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b,
                0x29, 0x2d, 0x2e, 0x30, 0x32, 0x34, 0x35, 0x38,
                0x39, 0x3a, 0x43, 0x44, 0x47, 0x48, 0x49, 0x4a,
                0x4b, 0x4c, 0x4d
            };
            for (int lnI = 0; lnI < 40; lnI++)
            {
                List<byte> cityWeapons = new List<byte>();
                int byteToUse = 0x8308 + (lnI * 27);
                // Weapons at bytes 0-4, armor at bytes 5-12, items at bytes 13-21.  I reserve the right to place weapons in armor stores and vice versa.
                for (int lnJ = 0; lnJ < 5; lnJ++)
                {
                    bool duplicate = true;
                    byte currentWeapon = 0;
                    while (duplicate)
                    {
                        currentWeapon = weapons[r1.Next() % weapons.Length];
                        duplicate = false;
                        for (int lnK = 0; lnK < cityWeapons.Count; lnK++)
                            if (currentWeapon == cityWeapons[lnK]) { duplicate = true; break; }
                    }
                    cityWeapons.Sort();
                    cityWeapons.Add(currentWeapon);
                }
                for (int lnJ = 0; lnJ < 5; lnJ++)
                    romData[byteToUse + lnJ] = cityWeapons[lnJ];

                List<byte> cityArmor = new List<byte>();
                for (int lnJ = 6; lnJ < 13; lnJ++)
                {
                    bool duplicate = true;
                    byte currentWeapon = 0;
                    while (duplicate)
                    {
                        currentWeapon = armor[r1.Next() % armor.Length];
                        duplicate = false;
                        for (int lnK = 0; lnK < cityArmor.Count; lnK++)
                            if (currentWeapon == cityArmor[lnK]) { duplicate = true; break; }
                    }
                    cityArmor.Sort();
                    cityArmor.Add(currentWeapon);
                }
                for (int lnJ = 6; lnJ < 13; lnJ++)
                    romData[byteToUse + lnJ] = cityArmor[lnJ - 6];

                List<byte> cityItems = new List<byte>();
                for (int lnJ = 13; lnJ < 22; lnJ++)
                {
                    bool duplicate = true;
                    byte currentItem = 0;
                    while (duplicate)
                    {
                        currentItem = items[r1.Next() % items.Length];
                        duplicate = false;
                        for (int lnK = 0; lnK < cityItems.Count; lnK++)
                        {
                            if (currentItem == cityItems[lnK]) { duplicate = true; break; }
                        }
                    }
                    cityItems.Sort();
                    cityItems.Add(currentItem);
                }
                for (int lnJ = 13; lnJ < 21; lnJ++)
                    romData[byteToUse + lnJ] = cityItems[lnJ - 13];
            }
        }

        private void randomizeWhoCanEquip(Random r1)
        {
            byte[] weapons = {
                1, 2, 3, 4, 5, 6, 7,
                8, 9, 10, 11, 12, 13, 14, 15,
                16, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                41, 42, 43, 44, 45, 46, 47,
                48, 49, 50
            };
            byte[] armor = {
                0, 1, 2, 3, 4, 5, 6, 7,
                8, 9, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28
            };
            byte[] accessory =
            {
                30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                40, 41, 42, 43,
                48, 49, 50, 51, 52
            };

            // Chances of equipping weapon:  Kamil, Olvan - 75%, Lejes - 50%, Esuna, Valsu - 35%, Lux, Wilme - 10%
            // Chances of equipping armor:  Kamil, Olvan - 60%, Esuna, Valsu, Lejes - 50%, Lux, Wilme - 10%
            // Chances of equipping accessory:  Kamil, Olvan - 75%, Esuna, Valsu - 40%, Lejes - 30%, Lux, Wilme - 10%
            // Kamil = 0x01, Olvan = 0x02, Esuna = 0x04, Wilme = 0x08, Lux = 0x10, Valsu = 0x20, Lejes = 0x40 - do not use 0x80.
            for (int lnI = 0; lnI < weapons.Length; lnI++)
            {
                int byteToUse = 0x639d + (10 * weapons[lnI]);
                byte whoEquip = 0;
                if (r1.Next() % 100 < 75) whoEquip += 0x01;
                if (r1.Next() % 100 < 75) whoEquip += 0x02;
                if (r1.Next() % 100 < 35) whoEquip += 0x04;
                if (r1.Next() % 100 < 10) whoEquip += 0x08;
                if (r1.Next() % 100 < 10) whoEquip += 0x10;
                if (r1.Next() % 100 < 35) whoEquip += 0x20;
                if (r1.Next() % 100 < 50) whoEquip += 0x40;
            }
            for (int lnI = 0; lnI < armor.Length; lnI++)
            {
                int byteToUse = 0x659b + (17 * armor[lnI]);
                byte whoEquip = 0;
                if (r1.Next() % 100 < 60) whoEquip += 0x01;
                if (r1.Next() % 100 < 60) whoEquip += 0x02;
                if (r1.Next() % 100 < 50) whoEquip += 0x04;
                if (r1.Next() % 100 < 10) whoEquip += 0x08;
                if (r1.Next() % 100 < 10) whoEquip += 0x10;
                if (r1.Next() % 100 < 50) whoEquip += 0x20;
                if (r1.Next() % 100 < 50) whoEquip += 0x40;
            }
            for (int lnI = 0; lnI < accessory.Length; lnI++)
            {
                int byteToUse = 0x659b + (17 * accessory[lnI]);
                byte whoEquip = 0;
                if (r1.Next() % 100 < 75) whoEquip += 0x01;
                if (r1.Next() % 100 < 75) whoEquip += 0x02;
                if (r1.Next() % 100 < 40) whoEquip += 0x04;
                if (r1.Next() % 100 < 10) whoEquip += 0x08;
                if (r1.Next() % 100 < 10) whoEquip += 0x10;
                if (r1.Next() % 100 < 40) whoEquip += 0x20;
                if (r1.Next() % 100 < 30) whoEquip += 0x40;
            }
        }

        private void boostExp()
        {
            for (int lnI = 0; lnI < 98; lnI++)
            {
                int byteToUse = 0x72f4 + (42 * lnI);
                int xp = romData[byteToUse + 34] + (256 * romData[byteToUse + 35]);
                xp *= (trkExperience.Value * 20 / 100);
                romData[byteToUse + 34] = (byte)(xp % 256);
                romData[byteToUse + 35] = (byte)(xp / 256);
            }
        }

        private void goldRequirements(Random r1)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtSeed.Text = (DateTime.Now.Ticks % 2147483647).ToString();

            try
            {
                using (TextReader reader = File.OpenText("last7th.txt"))
                {
                    txtFlags.Text = reader.ReadLine();
                    txtFileName.Text = reader.ReadLine();

                    determineChecks(null, null);

                    runChecksum();
                    loading = false;
                }
            }
            catch
            {
                // ignore error
                loading = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            using (StreamWriter writer = File.CreateText("last7th.txt"))
            {
                writer.WriteLine(txtFlags.Text);
                writer.WriteLine(txtFileName.Text);
            }
        }
    }
}
