﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Edi.CreditCardUtils.BINValidators;

namespace Edi.CreditCardUtils
{
    public class CreditCardValidator
    {
        /// <summary>
        /// Validate a credit card number according:
        /// 1. Number format (14-16 digits)
        /// 2. Luhn test
        /// 3. Specific BIN formats (optional)
        /// </summary>
        /// <param name="cardNumber">Card number</param>
        /// <param name="formatValidators">BIN format validators</param>
        /// <returns></returns>
        public static CreditCardValidationResult ValidCardNumber(
            string cardNumber, ICardTypeValidator[] formatValidators = null)
        {
            static CreditCardValidationResult CreateResult(CardNumberFormat format, string[] cardTypes = null)
            {
                return new CreditCardValidationResult
                {
                    CardNumberFormat = format,
                    CardTypes = cardTypes
                };
            }

            // Check card number length
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 14 || cardNumber.Length > 19)
            {
                return CreateResult(CardNumberFormat.Invalid_BadStringFormat);
            }

            // Check string is all numbers
            var isMatch = Regex.IsMatch(cardNumber, @"^\d*$");
            if (!isMatch)
            {
                return CreateResult(CardNumberFormat.Invalid_BadStringFormat);
            }

            // Try Luhn Test
            var digits = GetDigitsArrayFromCardNumber(cardNumber);
            if (!IsLuhnValid(digits))
            {
                return CreateResult(CardNumberFormat.Invalid_LuhnFailure);
            }

            // Test against known types
            var matchedCardTypes = new List<string>();
            foreach (var (key, value) in KnownCardTypes.Default)
            {
                if (Regex.IsMatch(cardNumber, value))
                {
                    matchedCardTypes.Add(key);
                }
            }

            // Test against type validator
            if (null != formatValidators)
            {
                var more = from validator in formatValidators
                           let brandMatch = Regex.IsMatch(cardNumber, validator.RegEx)
                           where brandMatch
                           select validator.Name;

                matchedCardTypes.AddRange(more);
            }

            return matchedCardTypes.Any() ? 
                CreateResult(CardNumberFormat.Valid_BINTest, matchedCardTypes.ToArray()) : 
                CreateResult(CardNumberFormat.Valid_LuhnOnly);
        }

        /// <summary>
        /// Check credit card numbers agaist Luhn Algorithm
        /// https://en.wikipedia.org/wiki/Luhn_algorithm
        /// </summary>
        /// <param name="digits">Credit card numbers</param>
        /// <returns>Is valid Luhn</returns>
        public static bool IsLuhnValid(int[] digits)
        {
            var sum = 0;
            var alt = false;
            for (var i = digits.Length - 1; i >= 0; i--)
            {
                if (alt)
                {
                    digits[i] *= 2;
                    if (digits[i] > 9)
                    {
                        digits[i] -= 9;
                    }
                }
                sum += digits[i];
                alt = !alt;
            }

            return sum % 10 == 0;
        }

        public static int[] GetDigitsArrayFromCardNumber(string cardNumber)
        {
            var digits = cardNumber.Select(p => int.Parse(p.ToString())).ToArray();
            return digits;
        }
    }
}
