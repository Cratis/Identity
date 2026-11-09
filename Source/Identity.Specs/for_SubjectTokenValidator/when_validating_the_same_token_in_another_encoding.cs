// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_the_same_token_in_another_encoding : given.a_subject_token_validator
{
    const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    SubjectTokenRefusal _padded;
    SubjectTokenRefusal _trailingBitsChanged;
    SubjectTokenRefusal _leadingWhitespace;
    SubjectTokenRefusal _trailingNewline;
    SubjectTokenRefusal _standardAlphabet;
    SubjectTokenRefusal _original;

    async Task Because()
    {
        var token = Valid();
        var signatureEnd = token.Length - 1;
        var flipped = Alphabet[Alphabet.IndexOf(token[signatureEnd]) ^ 1];

        _padded = await RefusalOf(token + "=");
        _trailingBitsChanged = await RefusalOf(token[..signatureEnd] + flipped);
        _leadingWhitespace = await RefusalOf(" " + token);
        _trailingNewline = await RefusalOf(token + "\n");
        _standardAlphabet = await RefusalOf(token.Replace('-', '+').Replace('_', '/') + "+");
        _original = await RefusalOf(token);
    }

    [Fact] void should_still_accept_the_canonical_token() => _original.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_refuse_padding() => _padded.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_changed_trailing_bits_of_the_signature() => _trailingBitsChanged.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_leading_whitespace() => _leadingWhitespace.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_a_trailing_newline() => _trailingNewline.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_characters_outside_the_url_safe_alphabet() => _standardAlphabet.ShouldEqual(SubjectTokenRefusal.Malformed);
}
