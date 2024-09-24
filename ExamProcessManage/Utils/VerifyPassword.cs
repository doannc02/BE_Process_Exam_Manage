using BCrypt.Net;

 public static class VerifyPassword
{

    public static bool VerifyPasswordBCrypt(string enteredPassword, string hashedPassword)
    {
        //string plainText = "admin123123";
        //string hashedPassword2  = BCrypt.Net.BCrypt.HashPassword(plainText);
        return BCrypt.Net.BCrypt.Verify(enteredPassword, hashedPassword);
    }

}


