import api from "@/utils/api";
const AuthService = {

  login: async (data : { username: string, password: string }) => {
    const { username, password } = data;
    try {
      const response = await api.post("/api/auth/login", {
        username,
        password,
      });
      return response;
    } catch (error: unknown) {
      throw error;
    }
  }

}
export default AuthService;
