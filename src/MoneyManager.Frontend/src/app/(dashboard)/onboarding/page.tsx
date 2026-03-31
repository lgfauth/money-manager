"use client";

import { useEffect } from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import { Rocket, Wallet, Tags, LayoutDashboard } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { apiClient } from "@/lib/api-client";

const steps = [
  {
    icon: Wallet,
    title: "Criar primeira conta",
    description: "Adicione uma conta bancaria, carteira ou cartao de credito",
    href: "/accounts",
  },
  {
    icon: Tags,
    title: "Configurar categorias",
    description: "Organize suas receitas e despesas em categorias",
    href: "/categories",
  },
  {
    icon: LayoutDashboard,
    title: "Ir para o Dashboard",
    description: "Veja o resumo das suas financas",
    href: "/",
  },
];

const container = {
  hidden: {},
  show: { transition: { staggerChildren: 0.15 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

export default function OnboardingPage() {
  useEffect(() => {
    apiClient.post("/api/onboarding/complete").catch(() => {});
  }, []);

  return (
    <div className="flex flex-col items-center justify-center py-12">
      <motion.div
        initial={{ scale: 0.8, y: -10 }}
        animate={{ scale: 1, y: 0 }}
        transition={{ type: "spring", stiffness: 200, damping: 15 }}
        className="mb-6"
      >
        <div className="flex h-20 w-20 items-center justify-center rounded-2xl bg-primary/10">
          <Rocket className="h-10 w-10 text-primary" />
        </div>
      </motion.div>

      <motion.h1
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.2 }}
        className="text-3xl font-bold"
      >
        Bem-vindo ao MoneyManager!
      </motion.h1>
      <motion.p
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.3 }}
        className="mt-2 text-muted-foreground"
      >
        Comece configurando sua conta em 3 passos simples
      </motion.p>

      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="mt-8 grid w-full max-w-lg gap-4"
      >
        {steps.map((step) => (
          <motion.div key={step.href} variants={item}>
            <Link href={step.href}>
              <Card className="rounded-xl hover:shadow-md transition-all hover:border-primary/30 cursor-pointer">
                <CardContent className="flex items-center gap-4 p-5">
                  <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-primary/10">
                    <step.icon className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <h3 className="font-semibold">{step.title}</h3>
                    <p className="text-sm text-muted-foreground">
                      {step.description}
                    </p>
                  </div>
                </CardContent>
              </Card>
            </Link>
          </motion.div>
        ))}
      </motion.div>
    </div>
  );
}
