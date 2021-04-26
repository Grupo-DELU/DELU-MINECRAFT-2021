using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DeluMC.Utils
{
    public class Block
    {

        public string name { get; }
        public (string, string)[] additionalProperties { get;  }

        public Block(string name, (string, string)[] properties = null)
        {
            this.name = name;
            additionalProperties = properties;
        }

        /// <summary>
        /// Minecraft's string representation for this block
        /// </summary>
        /// <returns></returns>
        public virtual string ToMcString()
        {
            var s = name;
            if (additionalProperties != null && additionalProperties.Length != 0)
            {
                s += "[";
                foreach (var (name, value) in additionalProperties)
                    s += $"{name}={value},";
                s += "]";
            }

            return s;
        }

        /// <summary>
        /// Parse a new block from a string
        /// </summary>
        /// <param name="block">valid string with a specified block</param>
        /// <returns>A block, with properties if it has some</returns>
        public static Block Parse(string block)
        {
            // Strip whitespaces
            block = block.Replace(" ", "");
            // To parse name
            var nameRegex = new Regex(@"\bminecraft:[\w_]+");
            // To parse square brackets
            var bracketsRegex = new Regex(@"\[.*\]"); 
            // To parse parameters
            var paramsRegex = new Regex(@"([\w_]+)=([\w_]+)");

            // Match names
            var nameMatch = nameRegex.Match(block);
            // Declare properties

            // Check if there's something inside square brackets
            if (bracketsRegex.Match(block) == null) 
                return new Block(nameMatch.Value, null);
            
            // get properties
            var paramsMatch = paramsRegex.Matches(block);
            var properties = new (string, string)[paramsMatch.Count];

            // Retrieve properties
            for (int i = 0; i < paramsMatch.Count; i++)
                properties[i] = (   paramsMatch[i].Groups[1].Value, 
                                    paramsMatch[i].Groups[2].Value
                                );

            return new Block(nameMatch.Value, properties);
        }
    }
}
