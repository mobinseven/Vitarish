import { defineConfig } from "orval";

const specsDir = "../specs";

export default defineConfig({
  authentication: {
    input: {
      target: `${specsDir}/authentication.json`,
    },
    output: {
      target: "./src/api/generated/authentication/authentication.ts",
      schemas: "./src/api/generated/authentication/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  users: {
    input: {
      target: `${specsDir}/users.json`,
    },
    output: {
      target: "./src/api/generated/users/users.ts",
      schemas: "./src/api/generated/users/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  sea: {
    input: {
      target: `${specsDir}/sea.json`,
    },
    output: {
      target: "./src/api/generated/sea/sea.ts",
      schemas: "./src/api/generated/sea/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  medical: {
    input: {
      target: `${specsDir}/medical.json`,
    },
    output: {
      target: "./src/api/generated/medical/medical.ts",
      schemas: "./src/api/generated/medical/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  parties: {
    input: {
      target: `${specsDir}/parties.json`,
    },
    output: {
      target: "./src/api/generated/parties/parties.ts",
      schemas: "./src/api/generated/parties/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  files: {
    input: {
      target: `${specsDir}/files.json`,
    },
    output: {
      target: "./src/api/generated/files/files.ts",
      schemas: "./src/api/generated/files/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  communications: {
    input: {
      target: `${specsDir}/communications.json`,
    },
    output: {
      target: "./src/api/generated/communications/communications.ts",
      schemas: "./src/api/generated/communications/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  appointments: {
    input: {
      target: `${specsDir}/appointments.json`,
    },
    output: {
      target: "./src/api/generated/appointments/appointments.ts",
      schemas: "./src/api/generated/appointments/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  search: {
    input: {
      target: `${specsDir}/search.json`,
    },
    output: {
      target: "./src/api/generated/search/search.ts",
      schemas: "./src/api/generated/search/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  intelligence: {
    input: {
      target: `${specsDir}/intelligence.json`,
    },
    output: {
      target: "./src/api/generated/intelligence/intelligence.ts",
      schemas: "./src/api/generated/intelligence/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  social: {
    input: {
      target: `${specsDir}/social.json`,
    },
    output: {
      target: "./src/api/generated/social/social.ts",
      schemas: "./src/api/generated/social/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  feed: {
    input: {
      target: `${specsDir}/feed.json`,
    },
    output: {
      target: "./src/api/generated/feed/feed.ts",
      schemas: "./src/api/generated/feed/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  knowledge: {
    input: {
      target: `${specsDir}/knowledge.json`,
    },
    output: {
      target: "./src/api/generated/knowledge/knowledge.ts",
      schemas: "./src/api/generated/knowledge/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  requests: {
    input: {
      target: `${specsDir}/requests.json`,
    },
    output: {
      target: "./src/api/generated/requests/requests.ts",
      schemas: "./src/api/generated/requests/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  commerce: {
    input: {
      target: `${specsDir}/commerce.json`,
    },
    output: {
      target: "./src/api/generated/commerce/commerce.ts",
      schemas: "./src/api/generated/commerce/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  clinic: {
    input: {
      target: `${specsDir}/clinic.json`,
    },
    output: {
      target: "./src/api/generated/clinic/clinic.ts",
      schemas: "./src/api/generated/clinic/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
});
