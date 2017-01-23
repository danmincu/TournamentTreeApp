using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

    namespace TournamentModels
    {
        public partial class Participant
        {
            internal List<Division> Divisions
            {
                get
                {
                    return this.ParticipantDivisionInts.Select(pint => pint.Division).ToList();
                }
            }
        }

        public partial class Division
        {
            bool IsPowerOfTwo(ulong x)
            {
                return (x & (x - 1)) == 0;
            }

            public List<Participant> OrderedParticipants
            {
                get
                {
                    return this.ParticipantDivisionInts.OrderBy(pdi => pdi.Participant.School.Name).OrderBy(pdi => pdi.Participant.Name).Select(pint => pint.Participant).ToList();
                }
            }

            public List<Participant> BracketParticipants(TournamentModels.TournamentTreeAppEntities context)
            {

                var allHaveOrder = !this.ParticipantDivisionInts.Any(pdi => pdi.OrderId == null);
                if (allHaveOrder)
                {
                    //get them in the order of the database              
                    var parray = this.ParticipantDivisionInts.OrderBy(pdi => pdi.OrderId).Select(pint => pint.Participant).ToList();
                    var h = (int)(Math.Pow(2, Math.Ceiling(Math.Log(parray.Count(), 2))));

                    if (parray.Count <= 2)
                        return parray;

                    var result = new List<Participant>();
                    int neededDummies = h - parray.Count();
                    while (parray.Count > 0)
                    {
                        var sp = parray.FirstOrDefault();
                        if (sp != null)
                        {
                            result.Add(sp);
                            parray.Remove(sp);

                            if (neededDummies-- > 0)
                            {
                                result.Add(new Participant() { Name = "     ===>"/*"advance ==>"*/, Dummy = true, School = null });
                            }
                        }
                    }
                    return result;
                }
                else
                {
                    ///this is necessary to have consistent output. however, ordering by GUID is equivalent with a random order that becomes consistent
                    var participants = this.ParticipantDivisionInts.OrderBy(pdi => pdi.ParticipantDivisionIntId).Select(pint => pint.Participant).ToList();

                    if (participants.Count <= 2)
                        return participants;

                    var result = new List<Participant>();

                    var parray = participants.OrderBy(p => p.School.SchoolId).ToList();
                    var h = (int)(Math.Pow(2, Math.Ceiling(Math.Log(parray.Count(), 2))));

                    var schoolsDescendentByCount = participants.GroupBy(n => n.School.SchoolId).
                         Select(group =>
                             new
                             {
                                 Id = group.Key,
                                 Count = group.Count()
                             }).OrderByDescending(sg => sg.Count).Select(sg => sg.Id).Distinct();


                    int order = 0;
                    int neededDummies = h - parray.Count();
                    while (parray.Count > 0)
                        foreach (var schoolId in schoolsDescendentByCount)
                        {
                            var sp = parray.FirstOrDefault(p => p.School.SchoolId == schoolId);
                            if (sp != null)
                            {
                                result.Add(sp);
                                parray.Remove(sp);

                                if (context != null)
                                {
                                    var pdiRec = this.ParticipantDivisionInts.FirstOrDefault(pdi => pdi.ParticipantId == sp.ParticipantId);
                                    if (pdiRec != null)
                                    {

                                        pdiRec.OrderId = order++;
                                        //save the new order to database
                                        context.SaveChanges();
                                    }
                                }

                                if (neededDummies-- > 0)
                                {
                                    result.Add(new Participant() { Name = "     ===>"/*"advance ==>"*/, Dummy = true, School = null });
                                }
                            }
                        }
                    return result;
                }
            }

           public object LinkHelper { set; get; }
    }

        public partial class Tournament
        {
            public static Tournament LoadFromImportedCsv(string tournamentName, string divisions, string data)
            {

                if (string.IsNullOrEmpty(tournamentName))
                    throw new ArgumentException("Tournament Name can't be null or empty", tournamentName);
                if (!divisions.StartsWith("Number,Title,Draw Bracket,Title,"))
                    throw new ArgumentException("divisions does not have the expected format. The first line should be <Number,Title,Draw Bracket,Title,>. Yours is different!", divisions);
                if (!(data.StartsWith("Name,School,") || data.StartsWith("Names,School,")))
                    throw new ArgumentException("data does not have the expected format. The first line should be <Name,School,>. Yours is different!", data);
                var result = new Tournament() { TournamentId = Guid.NewGuid(), Name = tournamentName };
                result.Divisions = new List<Division>();

                var i = 0;
                var lines = divisions.Replace("\r\n", "\n").Split('\n');
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(',');
                    if (!string.IsNullOrEmpty(parts[0]))
                        result.Divisions.Add(new Division()
                        {
                            DivisionId = Guid.NewGuid(),
                            TournamentId = result.TournamentId,
                            Id = parts[0].Trim(),
                            OrderId = i++,
                            Name = parts[1].Trim(),
                            DrawBracket = parts[2].Trim().Equals("Yes", StringComparison.InvariantCultureIgnoreCase),
                            Title = parts[3].Trim()
                        });
                }

                result.Schools = new List<School>();

                lines = data.Replace("\r\n", "\n").Split('\n');
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(',');
                    if (!string.IsNullOrEmpty(parts[1].Trim()) && !result.Schools.Any(s => s.Name.Equals(parts[1].Trim(), StringComparison.OrdinalIgnoreCase)))
                        result.Schools.Add(new School() { SchoolId = Guid.NewGuid(), TournamentId = result.TournamentId, Name = parts[1].Trim() });
                }

                result.Participants = new List<Participant>();
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(',');
                    if (!string.IsNullOrEmpty(parts[0].Trim()))
                    {
                        var participant = new Participant()
                        {
                            ParticipantId = Guid.NewGuid(),
                            TournamentId = result.TournamentId,
                            Name = parts[0].Trim(),
                            SchoolId = result.Schools.FirstOrDefault(s => s.Name.Equals(parts[1].Trim(), StringComparison.OrdinalIgnoreCase)).SchoolId,
                            //Divisions = AssociateDivisions(result.Divisions, parts, 2)
                        };

                        foreach (var division in AssociateDivisions(result.Divisions, parts, 2))
                        {
                            participant.ParticipantDivisionInts.Add(new ParticipantDivisionInt() { ParticipantDivisionIntId = Guid.NewGuid(), ParticipantId = participant.ParticipantId, DivisionId = division.DivisionId });
                        }

                        foreach (var pdId in participant.ParticipantDivisionInts.Distinct())
                        {
                            var division = result.Divisions.FirstOrDefault(d => d.DivisionId == pdId.DivisionId);
                            if (division != null)
                                division.ParticipantDivisionInts.Add(pdId);
                        }
                        result.Participants.Add(participant);
                    }
                }


                return result;
            }

            public static StringBuilder DebugReport(Tournament tournament)
            {
                var st = new StringBuilder();

                st.AppendLine("--------------------------------------------------------------------------------------------------------------------");
                st.AppendLine("T O U R N A M E N T");
                st.AppendLine("--------------------------------------------------------------------------------------------------------------------");
                st.AppendLine(tournament.Name);
                st.AppendLine("--------------------------------------------------------------------------------------------------------------------");
                st.AppendLine(" ");
                st.AppendLine(" ");
                st.AppendLine("------------------------------------------------------");
                st.AppendLine("S C H O O L S");
                st.AppendLine("------------------------------------------------------");
                foreach (var school in tournament.Schools.OrderBy(s => s.Name))
                {
                    st.AppendLine(" ");
                    st.AppendLine("------------------------------------------------------");
                    st.AppendLine("School name:" + school.Name);
                    st.AppendLine("------------------------------------------------------");
                    foreach (var participant in tournament.Participants.OrderBy(p => p.Name))
                    {
                        if (participant.School.SchoolId == school.SchoolId)
                            st.AppendLine(string.Format("   {0} {1}", participant.Name, participant.Divisions.Count == 0 ? "WARNING - no division assignment!" : string.Empty));

                    }
                    st.AppendLine("------------------------------------------------------");
                    st.AppendLine(" ");
                }
                st.AppendLine(" ");
                st.AppendLine(" ");
                st.AppendLine("------------------------------------------------------");
                st.AppendLine("D I V I S I O N S");
                st.AppendLine("------------------------------------------------------");
                foreach (var division in tournament.Divisions.OrderBy(d => d.OrderId))
                {
                    st.AppendLine(" ");
                    st.AppendLine("--------------------------------------------------------------------------------------------------------------------");
                    st.AppendLine(division.Name + (division.DrawBracket ? "" : " -- no bracket to be drawn"));
                    st.AppendLine("--------------------------------------------------------------------------------------------------------------------");
                    if (division.DrawBracket)
                    {
                        int i = 0;
                        foreach (var participant in division.BracketParticipants(null))
                        {
                            if (i % 2 == 0) st.AppendLine(" ");
                            st.AppendLine(String.Format("{0,45} {1,45}", participant.Name, ((participant.Dummy) ? "" : participant.School == null ? "" : participant.School.Name)));
                            if (i % 2 == 1) st.AppendLine(" ");
                            i++;
                        }
                    }
                    else
                        foreach (var participant in tournament.Participants.OrderBy(p => p.Name))
                        {
                            if (participant.Divisions.Any(d => d.Id == division.Id))
                                st.AppendLine(String.Format("{0,45} {1,45}", participant.Name, ((participant.Dummy) ? "" : participant.School == null ? "" : participant.School.Name)));
                        }

                }
                st.AppendLine("--------------------------------------------------------------------------------------------------------------------");
                st.AppendLine("");
                st.AppendLine("");
                st.AppendLine("....end of report....");
                return st;
            }

            private static List<Division> AssociateDivisions(IEnumerable<Division> divisionList, string[] parts, int startingIndex)
            {
                var result = new List<Division>();
                for (int i = startingIndex; i < parts.Length; i++)
                {
                    var division = divisionList.FirstOrDefault(d => d.Id == parts[i].Trim());
                    if (division != null && !result.Any(d => d.Id == division.Id))
                        result.Add(division);
                }
                return result;
            }
        }


        public class TournamentSingleDocumentModel
        {
            Tournament t;
            Guid divisionId;
            public TournamentSingleDocumentModel(Tournament t, Guid divisionId)
            {
                this.t = t;
                this.divisionId = divisionId;

            }
            public int PixHeight { set; get; }
            public int MaxDivisionCount { set; get; }
            public int StartDivisionPosition { set; get; }
            public List<Division> GetDivisions()
            {
                return this.t.Divisions.Where(d => d.DivisionId == this.divisionId).ToList();
            }
            public string Name
            {
                get { return t.Name; }
            }
        }

        public class TournamentModel
        {
            Tournament t;
            bool drawBracket;
            int minSize;
            int maxSize;
            public TournamentModel(Tournament t, bool drawBracket, int minSize, int maxSize)
            {
                this.t = t;
                this.drawBracket = drawBracket;
                this.minSize = minSize;
                this.maxSize = maxSize;
            }

            public int PixHeight { set; get; }
            public int MaxDivisionCount { set; get; }
            public int StartDivisionPosition { set; get; }
            public List<Division> GetDivisions()
            {
                return this.t.Divisions.Where(d => d.DrawBracket == this.drawBracket && Between(d.ParticipantDivisionInts.Count())).ToList();
            }
            public List<Division> GetDivisions(int position, int count)
            {
                return this.t.Divisions.Where(d => d.DrawBracket == this.drawBracket && Between(d.ParticipantDivisionInts.Count())).OrderBy(d => d.OrderId).Skip(position).Take(count).ToList();
            }


            private bool Between(int count)
            {
                return count >= minSize && count <= maxSize;
            }

            public string Name
            {
                get { return t.Name; }
            }

        }
    }
