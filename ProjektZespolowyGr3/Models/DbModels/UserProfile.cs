using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class UserProfile
    {
        public int Id { get; set; }
        public User _User { get; set; }
        public string base64Avatar { get; set; } = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAEnQAABJ0Ad5mH3gAAACdSURBVFhH7c47EsAgCABRb+7hPFgySZVZf5gApqDYxhF4Ked87CzxQVIppRv/zloC8NgszrcSAbh4Je5iUwAXvok7xQAu+hr3DwEc1og3ugAOasZb7oCrIYCfLfovgB8tC0AAAtAEeCGe9wJQAawRvNUEWCF4YwiwQHC/K4C7RQBNBPdOAVygFe9UAA5YVQH4wasbwEfPEh+82w44AeVhNAkPrpGnAAAAAElFTkSuQmCC";
        public string? base64Banner { get; set; }
        public string Description { get; set; } = "I'm a funny guy";

    }
}
