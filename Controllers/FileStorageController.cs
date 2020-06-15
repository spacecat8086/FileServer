using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace FileServer.Controllers
{
    [ApiController]
    [Route("[controller]/path/to")]
    public class FileStorageController : ControllerBase
    {
        private readonly ILogger<FileStorageController> _logger;
        private string root = @"D:\Files";

        public FileStorageController(ILogger<FileStorageController> logger)
        {
            _logger = logger;
        }


        [HttpGet()]
        [HttpGet("{*filename}")]
        public ActionResult GetFile(string filename)
        {
            if (isFile(filename))
            {
                try
                {
                    string fullpath = root + @"\" + filename;
                    FileStream fs = new FileStream(fullpath, FileMode.Open);
                    return File(fs, "application/unknown", filename);
                }
                catch { return BadRequest(); }
            }
            else
            {
                string directoryname = filename;
                try
                {
                    IReadOnlyCollection<string> files = Microsoft.VisualBasic.FileIO.FileSystem.GetFiles(root + @"\" + directoryname);
                    IReadOnlyCollection<string> directories = Microsoft.VisualBasic.FileIO.FileSystem.GetDirectories(root + @"\" + directoryname);
                    List<string> content = new List<string>(directories);
                    content.AddRange(files);
                    return new JsonResult(content, new JsonSerializerOptions { });
                }
                catch { return BadRequest(); }

            }
        }

        private bool isFile(string str)
        {
            try
            {
                if (str == null)
                    return false;
                int index = str.LastIndexOf("/") + 1;
                string substr = str.Substring(index, str.Length - index);
                if ((substr.Contains(".")) && (substr.IndexOf(".") == substr.LastIndexOf(".")))
                    return true;
                else
                    return false;
            }
            catch { return false; }
        }

        [HttpHead("{*filename}")]
        public ActionResult GetFileInfo(string filename)
        {
            try
            {
                string fileInfo = Microsoft.VisualBasic.FileIO.FileSystem.GetFileInfo(root + @"\" + filename).ToString();
                Response.Headers.Add("FullName", fileInfo);
                return Ok();
            }
            catch { return NotFound(); }
        }

        [HttpDelete("{*filename}")]
        public ActionResult DeleteFile(string filename)
        {
            try
            {
                if (isFile(filename))
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(root + @"\" + filename);
                }
                else
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(root + @"\" + filename,DeleteDirectoryOption.DeleteAllContents);
                }
                return Ok("Удалено!");
            }
            catch { return NotFound(); }
        }

        [HttpPut("{*filename}")]
        public ActionResult Put(IFormFileCollection uploads, string filename)
        {
            string fromcopy = Request.Form.FirstOrDefault(p => p.Key == "fromcopy").Value;
            if ((uploads.Count == 1)&&(fromcopy == null))                                   // либо перезаписываем один файл
            {
                return TryToPut(uploads, filename);
            }
            else if ((fromcopy != null)&&(uploads.Count != 1))                              // либо копируем один файл
            {
                return TryToCopy(filename, fromcopy);
            }
            else
                return BadRequest();
        }

        private ActionResult TryToPut(IFormFileCollection uploads, string filename)
        {
            string path = uploads[0].FileName;
            try
            {
                using (var fileStream = new FileStream(root + @"/" + filename, FileMode.Create))
                {
                    uploads[0].CopyTo(fileStream);
                }
                return Ok("Перезаписано!");
            }
            catch { return NotFound(); }
        }

        private ActionResult TryToCopy(string filename, string fromcopy)
        {
            if (isFile(fromcopy) && (isFile(filename)))
            {
                try
                {
                    string fullpath = root + @"\" + fromcopy;
                    using (var fileStream = new FileStream(root + @"/" + filename, FileMode.Create))
                    {
                        using (var fromCopyStream = new FileStream(root + @"/" + fromcopy, FileMode.Open))
                        {
                            fromCopyStream.CopyTo(fileStream);
                        }
                    }
                    return Ok("Скопировано!!");

                }
                catch { return NotFound(); }
            }
            else
                return BadRequest();
        }

    }
}
