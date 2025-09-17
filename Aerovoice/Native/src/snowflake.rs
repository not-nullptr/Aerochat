#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct Snowflake(u64);

impl Snowflake {
    pub fn from_u64(value: u64) -> Self {
        Snowflake(value)
    }
}
