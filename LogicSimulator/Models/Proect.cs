﻿using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LogicSimulator.Models {
    public class Proect: IComparable {
        public string Name { get; private set; }
        public long Created;
        public long Modified;

        public ObservableCollection<Diagram> schemes = new();
        public string? FileDir { get; private set; }
        public string? FileName { get; set; }

        private readonly Treatment parent;

        public Proect(Treatment parent) { 
            this.parent = parent;
            Name = "Новый проект";
            Created = Modified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            FileDir = null;
            FileName = null; 
            CreateScheme();
        }

        public Proect(Treatment parent, string dir, string fileName, object data) { // Импорт
            this.parent = parent;
            FileDir = dir;
            FileName = fileName;

            if (data is not Dictionary<string, object> dict) throw new Exception("Ожидался словарь в корне проекта");
            
            if (!dict.TryGetValue("name", out var value)) throw new Exception("В проекте нет имени");
            if (value is not string name) throw new Exception("Тип имени проекта - не строка");
            Name = name;

            if (!dict.TryGetValue("created", out var value2)) throw new Exception("В проекте нет времени создания");
            if (value2 is not int create_t) throw new Exception("Время создания проекта - не строка");
            Created = create_t;

            if (!dict.TryGetValue("modified", out var value3)) throw new Exception("В проекте нет времени изменения");
            if (value3 is not int mod_t) throw new Exception("Время изменения проекта - не строка");
            Modified = mod_t;

            if (!dict.TryGetValue("schemes", out var value4)) throw new Exception("В проекте нет списка схем");
            if (value4 is not List<object> arr) throw new Exception("Списко схем проекта - не массив строк");
            foreach (var s_data in arr) {
                if (s_data == null) throw new Exception("Одно из файловых имёт списка схем проекта - null");
                var scheme = new Diagram(this, s_data);
                schemes.Add(scheme);
            }
        }



        public Diagram CreateScheme() {
            var scheme = new Diagram(this);
            schemes.Add(scheme);
            Save();
            return scheme;
        }
        public Diagram AddScheme(Diagram? prev) {
            var scheme = new Diagram(this);
            int pos = prev == null ? 0 : schemes.IndexOf(prev) + 1;
            schemes.Insert(pos, scheme);
            Save();
            return scheme;
        }
        public void RemoveScheme(Diagram me) {
            schemes.Remove(me);
            Save();
        }
        public void UpdateList() {
            foreach (var scheme in schemes) scheme.UpdateProps();
        }

        public Diagram GetFirstScheme() => schemes[0];



        public object Export() {
            return new Dictionary<string, object> {
                ["name"] = Name,
                ["created"] = Created,
                ["modified"] = Modified,
                ["schemes"] = schemes.Select(x => x.Export()).ToArray(),
            };
        }

        public void Save() => Treatment.SaveProject(this);

        public int CompareTo(object? obj) {
            if (obj is not Proect proj) throw new ArgumentNullException(nameof(obj));
            return (int)(proj.Modified - Modified);
        }

        public override string ToString() {
            return Name + "\nИзменён: " + Modified.UnixTimeStampToString() + "\nСоздан: " + Created.UnixTimeStampToString();
        }

        internal void ChangeName(string name) {
            Name = name;
            Modified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Save();
        }

        public bool CanSave() => FileDir != null;
        public void SaveAs(Window mw) {
            FileDir = Treatment.RequestProjectPath(mw);
            Save();
            parent.AppendProject(this);
        }

        
        public void SetDir(string path) => FileDir = path;
    }
}
